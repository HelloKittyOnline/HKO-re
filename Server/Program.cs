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
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Serilog;
using Serilog.Extensions.Logging;
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

    internal static HashSet<Client> clients = new();
    internal static Dictionary<int, Request.ReceiveFunction> handlers;

    public static ILoggerFactory loggerFactory = LoggerFactory.Create(builder => {
#if DEBUG
        builder.SetMinimumLevel(LogLevel.Trace);
#else
        builder.SetMinimumLevel(LogLevel.Information);
#endif

        builder.AddConsole();

        var fileLogger = new LoggerConfiguration()
            .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.AddProvider(new SerilogLoggerProvider(fileLogger));
    });

    public static Serilog.Core.Logger ChatLogger = new LoggerConfiguration()
        .WriteTo.File("logs/chat.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Message:lj}{NewLine}")
        .CreateLogger();

    static async Task ListenClient(Client client, bool lobby) {
        try {
            Login.SendLobby(client, lobby);

            var head = new byte[5];

            while(true) {
                byte[] data; // todo: reuse buffer

                try {
                    if(await client.Stream.ReadAsync(head, client.Token) != 5 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                        break; // if count == 0 connection has been closed properly
                    }

                    var length = head[3] | head[4] << 8;
                    if(length > 16 * 1024 * 1024) {
                        break; // limit maximum package size to prevent oom attack
                    }

                    data = new byte[length];
                    if(await client.Stream.ReadAsync(data, client.Token) != length) {
                        break;
                    }
                } catch {
                    if(client.Token.IsCancellationRequested) {
                        // TODO: send disconnect reason?
                        // intentionally disconnected
                    } else {
                        // likely connection reset by peer or read timeout
                    }
                    break;
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
#if DEBUG
                if(id != 0x0063) { // skip ping
                    client.Logger.LogTrace("[{userID}] S <- C: {:X2}_{:X2} {data}", client.DiscordId, id >> 8, id & 0xFF, data);
                }
#endif

                var ms = new MemoryStream(data);
                client.Reader = new BinaryReader(ms);

                // skip id bytes
                client.ReadByte();
                client.ReadByte();

                // case 0x10_01: // Bingo
                // case 0x12_01: // 0052807b
                // case 0x12_02: // 005280f6

                if(handlers.TryGetValue(id, out var f)) {
                    try {
                        f(client);
                    } catch(NotImplementedException e) {
                        client.Logger.LogWarning("[{user}] Packet {major:X2}_{minor:X2} not implemented", client.DiscordId, data[0], data[1]);
                    } catch(Exception e) {
                        client.Logger.LogError(e, "[{userID}] Error handling packet {data}", client.DiscordId, data);
                        break;
                    }
                } else {
                    client.LogUnknown(data[0], data[1]);
                }
            }
        } catch(Exception e) {
            client.Logger.LogError(e, "[{userID}] Error: ", client.DiscordId);
        }

        lock(clients)
            clients.Remove(client);

        client.TcpClient.Close();
        IdManager.FreeId(client.Id);

        if(client.Username != null) {
            if(client.InGame) { // remove player from maps
                try {
                    Player.SendDeletePlayer(client.Player.Map.Players, client);

                    // TODO: kick other players from farm
                    /*foreach (var player in client.Player.Farm.Players) {
                        player.Player.ReturnFromFarm();
                        SendChangeMap(player);
                    }*/
                    maps.Remove(client.Player.Farm.Id); // remove farm from map list
                } catch { }
            }

            if(client.Player.MapType == 3 || client.Player.MapType == 4) {
                try {
                    client.Player.ReturnFromFarm();
                } catch { }
            }

            Database.LogOut(client.DiscordId, client.Player);
            client.Logger.LogInformation("[{userID}] Player {username} disconnected", client.DiscordId, client.Username);
        }
    }

    private static async Task Server(int port, bool lobby, CancellationToken token) {
        var server = new TcpListener(IPAddress.Any, port);
        server.Start();

        var logger = loggerFactory.CreateLogger("Server");
        logger.LogInformation($"Listening at :{port}");

        while(true) {
            TcpClient tcpClient;

            try {
                // throws if cancellation token is triggered
                tcpClient = await server.AcceptTcpClientAsync(token);
                tcpClient.ReceiveTimeout = 30 * 1000;
                tcpClient.SendTimeout = 30 * 1000;
            } catch when(token.IsCancellationRequested) {
                break;
            }

            logger.LogInformation($"Client {tcpClient.Client.RemoteEndPoint}");

            var client = new Client(tcpClient);
            lock(clients)
                clients.Add(client);

            client.RunTask = Task.Run(async () => {
                await ListenClient(client, lobby);
            });
        }
        server.Stop();
    }

    static void LoadData(string path) {
        var quests = ManualQuest.Load($"{path}/quests.json");
        minigameQuests = quests.Where(x => x.Minigame != null).ToDictionary(x => x.Minigame.Id);
        // order so that minigames have the least priority
        questMap = (Lookup<int, ManualQuest.Sub>)quests.OrderBy(x => x.Minigame != null).SelectMany(x => x.Sections).ToLookup(x => x.Npc);

        var archive = SeanArchive.Extract($"{path}/client_table_eng.sdb");
        byte[] GetItem(string name) => archive.First(x => x.Name == name).Contents;

        teleporters = SeanDatabase.Load<Teleport>(GetItem("teleport_list.txt"));
        resources   = SeanDatabase.Load<Extractor.Resource>(GetItem("res_list.txt"));
        lootTables  = SeanDatabase.Load<ResCounter>(GetItem("res_counter.txt"));
        items       = SeanDatabase.Load<ItemAtt>(GetItem("item_att.txt"));
        equipment   = SeanDatabase.Load<EquAtt>(GetItem("equ_att.txt"));
        skills      = SeanDatabase.Load<SkillInfo>(GetItem("skill_exp.txt"));
        checkpoints = SeanDatabase.Load<Checkpoint>(GetItem("check_point.txt"));
        mobAtts     = SeanDatabase.Load<MobAtt>(GetItem("mob_att.txt"));
        prodRules   = SeanDatabase.Load<ProdRule>(GetItem("prod_rule.txt"));
        seeds       = SeanDatabase.Load<Seed>(GetItem("seed_att.txt"));
        farms       = SeanDatabase.Load<FarmData>(GetItem("farm_list.txt"));
        npcEncyclopedia = SeanDatabase.Load<NpcEncyclopedia>(GetItem("npc_encyclopedia.txt")).Where(x => x.NpcId != 0).ToDictionary(x => x.NpcId, x => x.Id);

        for(int i = 0; i < lootTables.Length; i++) {
            lootTables[i].Init();
        }

        var mapList = SeanDatabase.Load<MapList>(GetItem("map_list.txt"));

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

        var serverLogger = loggerFactory.CreateLogger("Server");
        serverLogger.LogInformation("Starting server...");

        var serverTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            serverTokenSource.Cancel();
        };

        loggerFactory.CreateLogger("Server").LogInformation("Loading data...");

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
        await Server(25000, true, serverTokenSource.Token);

        serverLogger.LogInformation("Stopping server...");

        // start logging out all users
        foreach(var client in clients) {
            client.Close();
        }

        // wait for all users to finish logging out
        Task.WaitAll(clients.Select(x => x.RunTask).ToArray());
    }
}
