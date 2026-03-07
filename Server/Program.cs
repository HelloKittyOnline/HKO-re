using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using MySqlConnector;
using Serilog.Events;
using Server.Protocols;

namespace Server;

class Program {
    internal static ConcurrentDictionary<int, Instance> maps;

    internal static Dictionary<int, ManualQuest.Sub[]> questsByNPC;

    internal static Teleport[] teleporters;
    internal static Extractor.Resource[] resources;
    internal static ResCounter[] lootTables;
    internal static ItemAtt[] items;
    internal static EquAtt[] equipment;
    internal static SkillInfo[] skills;
    internal static ProdRule[] prodRules;
    internal static MobAtt[] mobAtts;
    internal static Checkpoint[] checkpoints;
    internal static Dictionary<int, Shop> Shops;
    internal static Dictionary<int, int> npcEncyclopedia;
    internal static Seed[] seeds;
    internal static FarmData[] farms;
    internal static BuildingAgreement[] buildings;
    internal static Furniture[] furniture;
    internal static FoodAtt[] food;

    internal static PetInitData[] petInitData;
    internal static PetFood[] petFood;
    // internal static PetLevelup[] petLevelup;
    internal static PetExp[] petExp;

    internal static ConcurrentDictionary<int, Client> clients = new();
    internal static Dictionary<int, Request.ReceiveFunction> handlers;

    static bool HandleRequest(Client client, Req r) {
        var major = r.ReadByte();
        var minor = r.ReadByte();

        var id = (major << 8) | minor;

        if(handlers.TryGetValue(id, out var f)) {
            try {
                // case 0x10_01: // Bingo
                // case 0x12_01: // 0052807b
                // case 0x12_02: // 005280f6

                f(ref r, client);
            } catch(NotImplementedException) {
                Logging.Logger.Debug("[{username}_{userID}] Packet not implemented {data}", client.Username, client.DiscordId, r.Buffer);
            } catch(Exception e) {
                Logging.Logger.Error(e, "[{username}_{userID}] Error handling packet {data}", client.Username, client.DiscordId, r.Buffer);
                return false;
            }
        } else {
            Logging.Logger.Error("[{username}_{userID}] Unknown Packet {data}", client.Username, client.DiscordId, r.Buffer);
        }

        return true;
    }

    static async Task RecieveLoop(Client client) {
        var head = new byte[5];

        while(true) {
            if(await client.TcpClient.Client.ReceiveAsync(head, client.Token) != 5 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                break; // if count == 0 connection has been closed properly
            }

            var length = head[3] | head[4] << 8;
            if(length > 16 * 1024 * 1024) {
                break; // limit maximum package size to prevent oom attack
            }

            var data = new byte[length];
            if(await client.TcpClient.Client.ReceiveAsync(data, client.Token) != length) {
                break;
            }

            if(Logging.Logger.IsEnabled(LogEventLevel.Verbose)) { // extra check to prevent boxing
#if DEBUG
                if(data.Length != 1 && !(data[0] == 0 && data[1] == 0x63)) // skip ping messages when in debug mode
#endif
                Logging.Logger.Verbose("[{username}_{userID}] S <- C: {data}", client.Username, client.DiscordId, data);
            }

            if(data.Length == 1) {
                if(data[0] == 0x7E) { // 005551be
                } else if(data[0] == 0x7F) { // 005bb8a9
                    var b = new PacketBuilder(); // reset timeout
                    b.WriteByte(0x7F);
                    b.Send(client);
                }
                continue;
            }

            var id = (data[0] << 8) | data[1];
            if(client.Username == null && id != 1) {
                break; // first package has to be login attempt
            }

            var r = new Req(data);
            if(!HandleRequest(client, r))
                break;
        }
    }

    static async Task ListenClient(Client client) {
        clients[client.Id] = client;

        Login.SendLobby(client);
        try {
            await RecieveLoop(client);
        } catch(Exception e) {
            if(client.Token.IsCancellationRequested) {
                Debug.Assert(e is OperationCanceledException);
            } else {
                Logging.Logger.Error(e, "[{username}_{userID}] Error:", client.Username, client.DiscordId);
            }
        }

        clients.Remove(client.Id, out var _);

        client.Close();
        client.TcpClient.Close();
        IdManager.FreeId(client.Id);

        if(client.Username == null)
            return; // not logged in so no need for cleanup

        if(client.InGame) {
            Debug.Assert(client.Player != null);
            client.InGame = false;

            // remove player from map
            Player.LeaveMap(client);

            /*
            // TODO: kick other players from farm/house
            foreach (var player in client.Player.Farm.Players) Player.ReturnFromFarm(player);
            foreach (var player in client.Player.Farm.House.Floor0.Players) Player.ReturnFromFarm(player);
            foreach (var player in client.Player.Farm.House.Floor1.Players) Player.ReturnFromFarm(player);
            foreach (var player in client.Player.Farm.House.Floor2.Players) Player.ReturnFromFarm(player);
            */

            // remove player associated maps
            maps.Remove(client.Player.Farm.Id, out var _);
            maps.Remove(client.Player.Farm.House.Floor0.Id, out var _);
            maps.Remove(client.Player.Farm.House.Floor1.Id, out var _);
            maps.Remove(client.Player.Farm.House.Floor2.Id, out var _);
        }

        Database.LogOut(client);
        Logging.Logger.Information("[{username}_{userID}] Player disconnected", client.Username, client.DiscordId);
    }

    private static async Task Server(int port, CancellationToken token) {
        var server = new TcpListener(IPAddress.Any, port);
        server.Start();

        Logging.Logger.Information("Listening at :{port}", port);

        while(true) {
            TcpClient tcpClient;

            try {
                // throws if cancellation token is triggered
                tcpClient = await server.AcceptTcpClientAsync(token);
            } catch(Exception e) {
                if(token.IsCancellationRequested) {
                    Debug.Assert(e is OperationCanceledException);
                } else {
                    Logging.Logger.Error(e, "AcceptTcpClientAsync failed");
                }
                break;
            }

            Logging.Logger.Information("Connection from {ip}", tcpClient.Client.RemoteEndPoint);

            var client = new Client(tcpClient);
            client.RunTask = Task.Run(() => ListenClient(client), client.Token);
        }
        server.Stop();
    }

    static void LoadData(string path, CancellationToken token) {
        var quests = ManualQuest.Load($"{path}/quests.json");
        // order so that minigames have the least priority
        questsByNPC = quests.OrderBy(x => x.Minigame != null).SelectMany(x => x.Sections).GroupBy(x => x.Npc).ToDictionary(x => x.Key, x => x.ToArray());

        var archive = SeanArchive.Extract($"{path}/client_table_eng.sdb");
        T[] GetItem<T>(string name) where T : struct => SeanDatabase.Load<T>(archive.First(x => x.Name == name).Contents);

        teleporters = GetItem<Teleport>("teleport_list.txt");
        resources   = GetItem<Extractor.Resource>("res_list.txt");
        lootTables  = GetItem<ResCounter>("res_counter.txt");
        items       = GetItem<ItemAtt>("item_att.txt");
        equipment   = GetItem<EquAtt>("equ_att.txt");
        skills      = GetItem<SkillInfo>("skill_exp.txt");
        checkpoints = GetItem<Checkpoint>("check_point.txt");
        mobAtts     = GetItem<MobAtt>("mob_att.txt");
        prodRules   = GetItem<ProdRule>("prod_rule.txt");
        seeds       = GetItem<Seed>("seed_att.txt");
        farms       = GetItem<FarmData>("farm_list.txt");
        buildings   = GetItem<BuildingAgreement>("building_agreement.txt");
        furniture   = GetItem<Furniture>("furniture_list.txt");
        food        = GetItem<FoodAtt>("food_att.txt");
        var mapList = GetItem<MapList>("map_list.txt");
        npcEncyclopedia = GetItem<NpcEncyclopedia>("npc_encyclopedia.txt").Where(x => x.NpcId != 0).ToDictionary(x => x.NpcId, x => x.Id);

        petInitData = GetItem<PetInitData>("pet_init_data.txt");
        // petLevelup = GetItem<PetLevelup>("pet_levelup.txt");
        petFood = GetItem<PetFood>("pet_food.txt");
        petExp = GetItem<PetExp>("pet_exp.txt");

        for(int i = 0; i < lootTables.Length; i++) {
            lootTables[i].Init();
        }

        var npcs = NpcData.Load($"{path}/npcs.json");
        var shops = Shop.Load($"{path}/shops.json");

        Shops = new Dictionary<int, Shop>();
        foreach(var shop in shops) {
            foreach(var npc in shop.Npcs) {
                Shops[npc] = shop;
            }
        }

        var mob_data = JsonNode.Parse(File.ReadAllText($"{path}/maps.json")).AsArray();
        var cutMobs = mobAtts.Where(x => x.Name != null).ToArray();

        // bundle all entities together to make lookup easier
        maps = new();
        for(int i = 0; i < mapList.Length; i++) {
            var item = mapList[i];
            if(item.Name == null)
                continue;

            var _mobs = mob_data.FirstOrDefault(x => (int)x["Id"] == i)?["Mobs"]?.AsArray();
            var mobs = new List<MobData>();
            if(_mobs != null) {
                for(var j = 0; j < _mobs.Count; j++) {
                    var mob = _mobs[(Index)j];
                    var id = (int)mob["MobId"];
                    if(id == 0)
                        throw new InvalidDataException("Missing mob id");

                    if((bool)mob["Cheer"]) {
                        // todo
                    } else {
                        mobs.Add(new MobData(
                            j + 1,
                            cutMobs[id - 1].Id,
                            (int)mob["X"],
                            (int)mob["Y"])
                        );
                    }
                }
            }

            if(i <= 7) {
                maps[i] = new DreamRoom {
                    Id = i,
                    MapData = item,
                    _mobs = mobs.ToArray(),
                    _npcs = npcs.Where(x => x.MapId == i).ToArray(),
                    _resources = resources.Where(x => x.MapId == i).ToArray(),
                    _teleporters = teleporters.Where(x => x.FromMap == i).ToArray(),
                    _checkpoints = checkpoints.Where(x => x.Map == i).ToArray()
                };
            } else {
                maps[i] = new StandardMap {
                    Id = i,
                    MapData = item,
                    _mobs = mobs.ToArray(),
                    _npcs = npcs.Where(x => x.MapId == i).ToArray(),
                    _resources = resources.Where(x => x.MapId == i).ToArray(),
                    _teleporters = teleporters.Where(x => x.FromMap == i).ToArray(),
                    _checkpoints = checkpoints.Where(x => x.Map == i).ToArray()
                };
            }
        }

        maps[50007] = maps[4]; // "dream room 4" for some reason

        Debug.Assert(teleporters.All(x => x.FromMap == 0 || maps[x.FromMap] != null)); // all teleporters registered
        Debug.Assert(npcs.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all npcs registered
        Debug.Assert(resources.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all resources registered
    }

    static async Task Main(string[] args) {
        handlers = Request.GetEndpoints();

        Logging.Logger.Information("Starting server...");

        var serverTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            serverTokenSource.Cancel();
        };

        Logging.Logger.Information("Loading data...");

        LoadData("./");

        // todo: add config file
        var sb = new MySqlConnectionStringBuilder {
            Server = "127.0.0.1",
            UserID = "root",
            Password = "",
            Port = 3306,
            Database = "hko"
        };

        Database.SetConnectionString(sb.ConnectionString);
        Commands.RunConsole();

        _ = Task.Run(() => Farm.FarmThread(serverTokenSource.Token), serverTokenSource.Token);
        _ = Task.Run(() => GameUpdateLoop(serverTokenSource.Token), serverTokenSource.Token);
        await Server(25000, serverTokenSource.Token);

        Logging.Logger.Information("Stopping server...");

        // start logging out all users
        foreach(var client in clients) {
            client.Value.Close();
        }

        // wait for all users to finish logging out
        foreach(var client in clients.Values) {
            try {
                client.RunTask.Wait();
            } catch(Exception e) {
                Logging.Logger.Error(e, "[{username}_{userID}] Error while waiting for clients to disconnect", client.Username, client.DiscordId);
            }
        }
    }

    public static async Task GameUpdateLoop(CancellationToken token) {
        // 500 ~ half screen width
        // 400 ~ half screen height
        // 640 ~ half screen diagonal

        // 1 step = 500ms
        long step = 0;
        const int dt = 2; // 2 steps per second

        while(true) {
            await Task.Delay(1000 / dt, token);

            if(clients.IsEmpty)
                continue;

            // player update
            foreach(var (_, client) in clients) {
                lock(client.Lock) {
                    if(!client.InGame || client.Player.Hp == 0)
                        continue;

                    var p = client.Player;

                    // every 2 seconds
                    if(step % 4 == 0) {
                        bool changed = false;

                        // regens while not in combat
                        if(p.CurrentAction != 1 && p.Hp < p.MaxHp) {
                            changed = true;
                            p.Hp++;
                        }

                        // only regens while not in action
                        if(p.CurrentAction == 0 && p.Sta < p.MaxSta) {
                            changed = true;
                            p.Sta++;
                        }

                        if(changed)
                            Player.SendPlayerHpSta(client);
                    }

                    // update approximate player position
                    var dx = p.TargetX - p.PositionX;
                    var dy = p.TargetY - p.PositionY;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if(dist != 0) {
                        var l = Math.Min(dist, p.Speed / dt);
                        p.PositionX += (int)(dx / dist * l);
                        p.PositionY += (int)(dy / dist * l);
                    }
                }
            }

            var activeMaps = clients.Select(x => x.Value).Where(x => x.InGame).ToLookup(x => x.Player.CurrentMap);
            foreach(var item in activeMaps) {
                if(!maps.TryGetValue(item.Key, out var map))
                    continue;

                var clients = item;

                foreach(var mob in map.Mobs) {
                    lock(mob) {
                        if(mob.Hp == 0)
                            continue; // sleeping

                        if(mob.Data.Aggressive && mob.Target == null) {
                            // check near players
                            foreach(var client in map.Players) {
                                if(!client.InGame || client.Player.Hp == 0)
                                    continue;

                                var dx = mob.X - client.Player.PositionX;
                                var dy = mob.Y - client.Player.PositionY;
                                if(Math.Sqrt((dx * dx) + (dy * dy)) < 100) {
                                    mob.Target = client;
                                    Battle.SendMobState(clients, mob, 2);
                                }
                            }
                        }

                        if(mob.Target != null) {
                            if(!mob.Target.InGame || mob.Target.Player.CurrentMap != map.Id) {
                                // player left game or map
                                mob.AbortFollow(clients);
                            } else {
                                var dx = mob.X - mob.Target.Player.PositionX;
                                var dy = mob.Y - mob.Target.Player.PositionY;
                                var pd = Math.Sqrt((dx * dx) + (dy * dy));

                                if(pd < 75) {
                                    if(pd < 25) { // too close. move back to circle around player
                                        if(pd == 0) {
                                            dx = 1;
                                            pd = 1;
                                        }
                                        mob.X = mob.Target.Player.PositionX + (int)(dx / (float)pd * 50);
                                        mob.Y = mob.Target.Player.PositionY + (int)(dy / (float)pd * 50);
                                        Battle.SendMobMove(clients, mob, mob.Speed * 2);
                                    }

                                    // attack
                                    if(step % 2 == 0)
                                        mob.AttackTarget(clients);
                                } else {
                                    // move within 5 units of player
                                    var l = Math.Max(pd - 5, mob.Speed * 2 / dt);
                                    mob.X -= (int)(dx / (float)pd * l);
                                    mob.Y -= (int)(dy / (float)pd * l);

                                    if(mob.DistanceToSpawn() > 400) {
                                        mob.AbortFollow(clients);
                                    } else {
                                        Battle.SendMobMove(clients, mob, mob.Speed * 2);
                                    }
                                }
                            }
                        } else if(step % 8 == 0) { // move every 4 seconds
                            _ = Task.Delay(mob.MoveDelay + Random.Shared.Next(100, 2000)).ContinueWith((t) => MobData.RandomMove(mob, map.Players));
                        }
                    }
                }
            }

            step++;
        }
    }
}
