using System;
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
    internal static Dictionary<int, Instance> maps;

    internal static Dictionary<int, ManualQuest> minigameQuests;
    internal static Lookup<int, ManualQuest.Sub> questMap;

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

    internal static HashSet<Client> clients = new();
    internal static Dictionary<int, Request.ReceiveFunction> handlers;

    static async Task<byte[]> ReadPackage(Client client, byte[] head) {
        try {
            if(await client.Stream.ReadAsync(head, client.Token) != 5 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                return null; // if count == 0 connection has been closed properly
            }

            var length = head[3] | head[4] << 8;
            if(length > 16 * 1024 * 1024) {
                return null; // limit maximum package size to prevent oom attack
            }

            var data = new byte[length];
            if(await client.Stream.ReadAsync(data, client.Token) != length) {
                return null;
            }
            return data;
        } catch {
            if(client.Token.IsCancellationRequested) {
                // TODO: send disconnect reason?
                // intentionally disconnected
            } else {
                // likely connection reset by peer or read timeout
            }
            return null;
        }
    }

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
            } catch(NotImplementedException e) {
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
            var data = await ReadPackage(client, head);
            if(data == null)
                break;

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
        lock(clients)
            clients.Add(client);

        try {
            Login.SendLobby(client);
            await RecieveLoop(client);
        } catch(Exception e) {
            Logging.Logger.Error(e, "[{username}_{userID}] Error:", client.Username, client.DiscordId);
        }

        lock(clients)
            clients.Remove(client);

        client.TcpClient.Close();
        IdManager.FreeId(client.Id);

        if(client.Username != null) {
            if(client.InGame) { // remove player from maps
                Player.LeaveMap(client);

                try {
                    // TODO: kick other players from farm/house
                    /*foreach (var player in client.Player.Farm.Players) {
                        Player.ReturnFromFarm(player);
                    }*/
                    // remove player associated maps
                    maps.Remove(client.Player.Farm.Id);
                    maps.Remove(client.Player.Farm.House.Floor0.Id);
                    maps.Remove(client.Player.Farm.House.Floor1.Id);
                    maps.Remove(client.Player.Farm.House.Floor2.Id);
                } catch { }
            }

            if(client.Player.MapType is 3 or 4) {
                var map = maps[client.Player.ReturnMap];
                if(map is StandardMap s) {
                    client.Player.CurrentMap = s.Id;
                    client.Player.PositionX = s.MapData.FarmX;
                    client.Player.PositionY = s.MapData.FarmY;
                } else {
                    // put player back to sanrio harbour
                    Logging.Logger.Error("[{username}_{userID}] Failed to return from farm {mapId}", client.Username, client.DiscordId, client.Player.CurrentMap);
                    client.Player.CurrentMap = 8;
                    client.Player.PositionX = 7705;
                    client.Player.PositionY = 6007;
                }
            }

            Database.LogOut(client.DiscordId, client.Player);
            Logging.Logger.Information("[{username}_{userID}] Player disconnected", client.Username, client.DiscordId);
        }
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
                tcpClient.ReceiveTimeout = 20 * 1000;
                tcpClient.SendTimeout = 20 * 1000;
            } catch when(token.IsCancellationRequested) {
                break;
            }

            Logging.Logger.Information("Connection from {ip}", tcpClient.Client.RemoteEndPoint);

            var client = new Client(tcpClient);
            client.RunTask = Task.Run(() => ListenClient(client));
        }
        server.Stop();
    }

    static void LoadData(string path) {
        var quests = ManualQuest.Load($"{path}/quests.json");
        minigameQuests = quests.Where(x => x.Minigame != null).ToDictionary(x => x.Minigame.Id);
        // order so that minigames have the least priority
        questMap = (Lookup<int, ManualQuest.Sub>)quests.OrderBy(x => x.Minigame != null).SelectMany(x => x.Sections).ToLookup(x => x.Npc);

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
        var mapList = GetItem<MapList>("map_list.txt");
        npcEncyclopedia = GetItem<NpcEncyclopedia>("npc_encyclopedia.txt").Where(x => x.NpcId != 0).ToDictionary(x => x.NpcId, x => x.Id);

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

        Task.Run(Farm.FarmThread);
        await Server(25000, serverTokenSource.Token);

        Logging.Logger.Information("Stopping server...");

        // start logging out all users
        foreach(var client in clients) {
            client.Close();
        }

        // wait for all users to finish logging out
        var tasks = clients.ToArray();
        for (int i = 0; i < tasks.Length; i++) {
            var client = tasks[i];
            try {
                client.RunTask.Wait();
            } catch (Exception e) {
                Logging.Logger.Error(e, "[{username}_{userID}] Error while waiting for clients to disconnect", client.Username, client.DiscordId);
            }
        }
    }
}
