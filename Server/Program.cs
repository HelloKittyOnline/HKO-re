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
using MySql.Data.MySqlClient;
using Server.Protocols;

namespace Server {
    class Program {
        internal static MapData[] maps;
        internal static Lookup<int, ManualQuest.Sub> questMap;

        internal static Teleport[] teleporters;
        internal static Extractor.Resource[] resources;
        internal static ResCounter[] lootTables;
        internal static ItemAtt[] items;
        internal static EquAtt[] equipment;
        internal static SkillInfo[] skills;
        internal static ProdRule[] prodRules;
        internal static MobAtt[] mobAtts;
        internal static Dictionary<int, Shop> Shops;

        internal static List<Client> clients = new List<Client>();

        public static ILoggerFactory loggerFactory = LoggerFactory.Create(builder => {
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Trace);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif

            builder.AddConsole();
            // builder.AddProvider(new FileLoggerProvider());
        });

        static void ListenClient(Client client, bool lobby) {
            Login.SendLobby(client, lobby);

            while(true) {
                byte[] data; // todo: reuse buffer

                try {
                    var head = new byte[5];

                    if(client.Stream.ReadAsync(head, 0, 5, client.Token).Result != 5 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                        break;
                    }

                    var length = head[3] | head[4] << 8;

                    data = new byte[length];
                    if(client.Stream.ReadAsync(data, 0, length, client.Token).Result != length) {
                        break;
                    }
                } catch {
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
                    var str = $"S <- C: {id >> 8:X2}_{id & 0xFF:X2}";
                    if(data.Length > 2) {
                        str += "\n";
                        str += BitConverter.ToString(data, 2);
                    }
                    client.Logger.LogTrace(str);
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
                        case 0x1A:
                            MoleAwarenessEvent.Handle(client);
                            break;
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
                            client.Logger.LogWarning($"Unknown Packet {data[0]:X2}_{data[1]:X2}");
                            break;
                    }
                } catch(Exception e) {
                    client.Logger.LogError(e, BitConverter.ToString(data));
                    break;
                }
            }
        }

        private static void Server(int port, bool lobby, CancellationToken token) {
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();
            token.Register(server.Stop);

            var logger = loggerFactory.CreateLogger("Server");
            logger.LogInformation($"Listening at :{port}");

            while(true) {
                TcpClient tcpClient;
                try {
                    // throws if cancellation token is triggered
                    var task = server.AcceptTcpClientAsync();
                    task.Wait(token);
                    tcpClient = task.Result;
                } catch when(token.IsCancellationRequested) {
                    break;
                }

                logger.LogInformation($"Client {tcpClient.Client.RemoteEndPoint} {tcpClient.Client.LocalEndPoint}");

                var client = new Client(tcpClient);
                clients.Add(client);

                client.RunTask = Task.Run(() => {
                    try {
                        ListenClient(client, lobby);
                    } catch(Exception e) {
                        client.Logger.LogError(e, null);
                    }

                    client.TcpClient.Close();

                    if(client.Username != null) {
                        Database.LogOut(client.Username, client.Player);
                        client.Logger.LogInformation($"Player {client.Username} disconnected");

                        if(client.InGame) // remove player from maps
                            Player.BroadcastDeletePlayer(client.Player.Map.Players, client);
                    }

                    clients.Remove(client);
                });
            }
        }

        static void Main(string[] args) {
            loggerFactory.CreateLogger("Server").LogInformation("Starting server...");

            var serverTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;

                foreach(var client in clients) {
                    client.Close();
                }

                Task.WaitAll(clients.Select(x => x.RunTask).ToArray());
                serverTokenSource.Cancel();
            };

            loggerFactory.CreateLogger("Server").LogInformation("Loading data...");

            var quests = ManualQuest.Load("D:/Daten/Desktop/quests_test.json");
            questMap = (Lookup<int, ManualQuest.Sub>)quests.SelectMany(x => x.Start.Concat(x.End)).ToLookup(x => x.Npc);

            var archive = SeanArchive.Extract("./client_table_eng.sdb");

            teleporters = SeanDatabase.Load<Teleport>(archive.First(x => x.Name == "teleport_list.txt").Contents);
            resources   = SeanDatabase.Load<Extractor.Resource>(archive.First(x => x.Name == "res_list.txt").Contents);
            lootTables  = ResCounter.Load(archive.First(x => x.Name == "res_counter.txt"));
            items       = SeanDatabase.Load<ItemAtt>(archive.First(x => x.Name == "item_att.txt").Contents);
            equipment   = SeanDatabase.Load<EquAtt>(archive.First(x => x.Name == "equ_att.txt").Contents);
            skills      = SeanDatabase.Load<SkillInfo>(archive.First(x => x.Name == "skill_exp.txt").Contents);
            prodRules   = ProdRule.Load(archive.First(x => x.Name == "prod_rule.txt"));

            var mapList = SeanDatabase.Load<MapList>(archive.First(x => x.Name == "map_list.txt").Contents);
            mobAtts = SeanDatabase.Load<MobAtt>(archive.First(x => x.Name == "mob_att.txt").Contents);

            var npcs = NpcData.Load("./npc_data.json");
            var shops = Shop.Load("./shop_data.json");

            Shops = new Dictionary<int, Shop>();
            foreach(var shop in shops) {
                foreach(var npc in shop?.Npcs) {
                    Shops[npc] = shop;
                }
            }

            var mob_data = JsonNode.Parse(File.ReadAllText("./mob_data.json"));
            var cutMobs = mobAtts.Where(x => x.Name != null).ToArray();

            // bundle all entities together to make lookup easier
            maps = new MapData[mapList.Length];
            for(int i = 0; i < mapList.Length; i++) {
                var item = mapList[i];
                if(item.Name == null)
                    continue;

                var _mobs = mob_data.AsArray().FirstOrDefault(x => (int)x["Id"] == i)?["Mobs"].AsArray();
                var mobs = new List<MobData>();
                if(_mobs != null) {
                    mobs.AddRange(_mobs.Select((mob, j) => new MobData(j + 1, cutMobs[(int)mob["MobId"]].Id, (int)mob["X"], (int)mob["Y"])));
                }

                maps[i] = new MapData {
                    Id = i,
                    Mobs = mobs.ToArray(),
                    Npcs = npcs.Where(x => x.MapId == i).ToArray(),
                    Resources = resources.Where(x => x.MapId == i).ToArray(),
                    Teleporters = teleporters.Where(x => x.FromMap == i).ToArray()
                };
            }

            Debug.Assert(teleporters.All(x => x.FromMap == 0 || maps[x.FromMap] != null)); // all teleporters registered
            Debug.Assert(npcs.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all npcs registered
            Debug.Assert(resources.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all resources registered

            var sb = new MySqlConnectionStringBuilder {
                Server = "127.0.0.1",
                UserID = "root",
                Password = "",
                Port = 3306,
                Database = "hko"
            };

            Database.SetConnectionString(sb.ConnectionString);

            Server(25000, true, serverTokenSource.Token);
        }
    }
}
