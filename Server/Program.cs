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
    internal static MapData[] maps;

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

    internal static HashSet<Client> clients = new();

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
        Login.SendLobby(client, lobby);

        var head = new byte[5];

        while(true) {
            byte[] data; // todo: reuse buffer

            try {
                if(await client.Stream.ReadAsync(head, client.Token) != 5 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                    break;
                }

                var length = head[3] | head[4] << 8;

                data = new byte[length];
                if(await client.Stream.ReadAsync(data, client.Token) != length) {
                    break;
                }
            } catch {
                // if(client.Token.IsCancellationRequested) // TODO: send disconnect reason?
                // network error likely connection reset by peer or action canceled
                break;
            }

            if(data.Length == 1) {
                if(data[0] == 0x7E) { // 005551be
                } else if(data[0] == 0x7F) { // 005bb8a9
                    // reset timeout
                    var b = new PacketBuilder();

                    b.WriteByte(0x7F);

                    b.Send(client);
                }
                continue;
            }

            var id = (data[0] << 8) | data[1];

            if(client.Username == null && id != 1) {
                // invalid data
                break;
            }
#if DEBUG
            if(id != 0x0063) { // skip ping
                client.Logger.LogTrace("[{userID}] S <- C: {:X2}_{:X2} {data}", client.DiscordId, id >> 8, id & 0xFF, data);
            }
#endif

            var ms = new MemoryStream(data);
            client.Reader = new BinaryReader(ms);

            try {
                switch(client.ReadByte()) {
                    case 0x00:
                        Login.Handle(client);
                        break;
                    case 0x01:
                        CreateRole.Handle(client);
                        break;
                    case 0x02:
                        Player.Handle(client);
                        break;
                    case 0x03:
                        Chat.Handle(client);
                        break;
                    case 0x04:
                        Friend.Handle(client);
                        break;
                    case 0x05:
                        Npc.Handle(client);
                        break;
                    case 0x06:
                        Protocols.Resource.Handle(client);
                        break;
                    case 0x07:
                        Production.Handle(client);
                        break;
                    case 0x08:
                        Trade.Handle(client);
                        break;
                    case 0x09:
                        Inventory.Handle(client);
                        break;
                    case 0x0A:
                        Farm.Handle(client);
                        break;
                    case 0x0B:
                        Store.Handle(client);
                        break;
                    case 0x0C:
                        Battle.Handle(client);
                        break;
                    case 0x0D:
                        Pet.Handle(client);
                        break;
                    case 0x0E:
                        Guild.Handle(client);
                        break;
                    case 0x0F:
                        Hompy.Handle(client);
                        break;
                    // case 0x10: Bingo.Handle(client); break;
                    // case 0x10_01: //
                    case 0x11:
                        NPCWalk.Handle(client);
                        break;
                    // case 0x12: ???
                    // case 0x12_01: // 0052807b
                    // case 0x12_02: // 005280f6
                    case 0x13:
                        Group.Handle(client);
                        break;
                    case 0x14:
                        Redeem.Handle(client);
                        break;
                    case 0x15:
                        Food4Friends.Handle(client);
                        break;
                    case 0x16:
                        Tutorial.Handle(client);
                        break;
                    case 0x17:
                        PODLeaderboard.Handle(client);
                        break;
                    case 0x18:
                        PODHousing.Handle(client);
                        break;
                    case 0x19:
                        EarthDay.Handle(client);
                        break;
                    // case 0x1A: MoleAwarenessEvent.Handle(client); break;
                    case 0x1B:
                        Achievement.Handle(client);
                        break;
                    case 0x1C:
                        Flash.Handle(client);
                        break;
                    case 0x1F:
                        CityCookOff.Handle(client);
                        break;
                    case 0x20:
                        ChocolateDefense.Handle(client);
                        break;
                    case 0x21:
                        Cheer.Handle(client);
                        break;
                    case 0x22:
                        BirthdayEvent.Handle(client);
                        break;
                    case 0x23:
                        Mail.Handle(client);
                        break;
                    default:
                        client.LogUnknown(data[0], data[1]);
                        break;
                }
            } catch(Exception e) {
                client.Logger.LogError(e, "[{userID}] Error handling packet {data}", client.DiscordId, data);
                break;
            }
        }
    }

    private static void Server(int port, bool lobby, CancellationToken token) {
        var server = new TcpListener(IPAddress.Any, port);
        server.Start();

        var logger = loggerFactory.CreateLogger("Server");
        logger.LogInformation($"Listening at :{port}");

        while(true) {
            TcpClient tcpClient;

            try {
                // throws if cancellation token is triggered
                tcpClient = server.AcceptTcpClientAsync(token).AsTask().Result;
            } catch when(token.IsCancellationRequested) {
                break;
            }

            logger.LogInformation($"Client {tcpClient.Client.RemoteEndPoint} {tcpClient.Client.LocalEndPoint}");

            var client = new Client(tcpClient);

            lock(clients)
                clients.Add(client);

            client.RunTask = Task.Run(async () => {
                try {
                    await ListenClient(client, lobby);
                } catch(Exception e) {
                    client.Logger.LogError(e, "[{userID}] Error: ", client.DiscordId);
                }
                client.TcpClient.Close();

                lock(clients)
                    clients.Remove(client);
                IdManager.FreeId(client.Id);

                if(client.Username != null) {
                    Database.LogOut(client.Username, client.Player);
                    client.Logger.LogInformation("[{userID}] Player {username} disconnected", client.DiscordId, client.Username);

                    if(client.InGame) // remove player from maps
                        Player.SendDeletePlayer(client.Player.Map.Players, client);
                }
            });
        }
        server.Stop();
    }

    static void LoadData(string path) {
        var quests = ManualQuest.Load($"{path}/quests.json");
        minigameQuests = quests.Where(x => x.Minigame != null).ToDictionary(x => x.Minigame.Id);
        // order so that minigames have the least priority
        questMap = (Lookup<int, ManualQuest.Sub>)quests.OrderBy(x => x.Minigame != null).SelectMany(x => x.Start.Concat(x.End)).ToLookup(x => x.Npc);

        var archive = SeanArchive.Extract($"{path}/client_table_eng.sdb");
        byte[] GetItem(string name) => archive.First(x => x.Name == name).Contents;

        teleporters = SeanDatabase.Load<Teleport>(GetItem("teleport_list.txt"));
        resources   = SeanDatabase.Load<Extractor.Resource>(GetItem("res_list.txt"));
        lootTables  = ResCounter.Load(GetItem("res_counter.txt"));
        items       = SeanDatabase.Load<ItemAtt>(GetItem("item_att.txt"));
        equipment   = SeanDatabase.Load<EquAtt>(GetItem("equ_att.txt"));
        skills      = SeanDatabase.Load<SkillInfo>(GetItem("skill_exp.txt"));
        checkpoints = SeanDatabase.Load<Checkpoint>(GetItem("check_point.txt"));
        mobAtts     = SeanDatabase.Load<MobAtt>(GetItem("mob_att.txt"));
        prodRules   = ProdRule.Load(GetItem("prod_rule.txt"));
        npcEncyclopedia = SeanDatabase.Load<NpcEncyclopedia>(GetItem("npc_encyclopedia.txt")).Where(x => x.NpcId != 0).ToDictionary(x => x.NpcId, x => x.Id);

        var mapList = SeanDatabase.Load<MapList>(GetItem("map_list.txt"));

        var npcs = NpcData.Load($"{path}/npc_data.json");
        var shops = Shop.Load($"{path}/shop_data.json");

        Shops = new Dictionary<int, Shop>();
        foreach(var shop in shops) {
            foreach(var npc in shop.Npcs) {
                Shops[npc] = shop;
            }
        }

        var mob_data = JsonNode.Parse(File.ReadAllText($"{path}/mob_data.json")).AsArray();
        var cutMobs = mobAtts.Where(x => x.Name != null).ToArray();

        // bundle all entities together to make lookup easier
        maps = new MapData[mapList.Length];
        for(int i = 0; i < mapList.Length; i++) {
            var item = mapList[i];
            if(item.Name == null)
                continue;

            var _mobs = mob_data.FirstOrDefault(x => (int)x["Id"] == i)?["Mobs"].AsArray();
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

            maps[i] = new MapData {
                Id = i,
                Mobs = mobs.ToArray(),
                Npcs = npcs.Where(x => x.MapId == i).ToArray(),
                Resources = resources.Where(x => x.MapId == i).ToArray(),
                Teleporters = teleporters.Where(x => x.FromMap == i).ToArray(),
                Checkpoints = checkpoints.Where(x => x.Map == i).ToArray()
            };
        }

        Debug.Assert(teleporters.All(x => x.FromMap == 0 || maps[x.FromMap] != null)); // all teleporters registered
        Debug.Assert(npcs.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all npcs registered
        Debug.Assert(resources.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all resources registered
    }

    static void Main(string[] args) {
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

        Server(25000, true, serverTokenSource.Token);

        serverLogger.LogInformation("Stopping server...");

        foreach(var client in clients) {
            client.Close();
        }

        Task.WaitAll(clients.Select(x => x.RunTask).ToArray());
    }
}
