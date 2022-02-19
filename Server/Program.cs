using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Server.Protocols;

namespace Server {
    class MapData {
        public Teleport[] Teleporters { get; set; }
        public NPCName[] Npcs { get; set; }
        public Extractor.Resource[] Resources { get; set; }
    }

    struct DialogData {
        public int Id { get; set; }
        public int Quest { get; set; }
        public bool Begins { get; set; }
        public int Previous { get; set; }
    }

    class Program {
        internal static MapData[] maps;
        internal static Dictionary<int, DialogData[]> dialogData;

        internal static Teleport[] teleporters;
        // internal static NPCName[] npcs;
        internal static Extractor.Resource[] resources;
        internal static ResCounter[] lootTables;
        internal static ItemAtt[] items;
        internal static EquAtt[] equipment;
        internal static Quest[] quests;
        internal static SkillInfo[] skills;

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

            var reader = new BinaryReader(client.Stream, Encoding.Default, true);

            while(true) {
                var head = new byte[3];

                var task = client.Stream.ReadAsync(head, 0, 3, client.Token);
                task.Wait();
                if(task.Result != 3 || head[0] != '^' || head[1] != '%' || head[2] != '*') {
                    break;
                }

                var data = reader.ReadBytes(reader.ReadUInt16());

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
                        case 0x09:
                            Inventory.Handle(client);
                            break;
                        case 0x0C:
                            Battle.Handle(client);
                            break;
                        case 0x13:
                            Group.Handle(client);
                            break;
                        /*
                        case 0x07_01: // 00529917
                        case 0x07_04: // 005299a6

                        case 0x08_01: // trade invite
                        case 0x08_02: //
                        case 0x08_03: //
                        case 0x08_04: //
                        case 0x08_06: //

                        case 0x0A_01: // 00581350
                        case 0x0A_02: // 005813c4
                        case 0x0A_03: // 0058148c
                        case 0x0A_04: // 00581504
                        case 0x0A_05: //
                        case 0x0A_06: //
                        case 0x0A_07: //
                        case 0x0A_08: //
                        case 0x0A_09: //
                        case 0x0A_0B: //
                        case 0x0A_0C: //
                        case 0x0A_0E: //
                        case 0x0A_0F: //
                        case 0x0A_16: //
                        case 0x0A_18: //
                        case 0x0A_19: //
                        case 0x0A_1A: //
                        case 0x0A_1B: //
                        case 0x0A_1C: //
                        case 0x0A_24: //
                        case 0x0A_25: //

                        case 0x0B_01: // 0054d96c
                        case 0x0B_02: //
                        case 0x0B_03: //

                        case 0x0D_02: // 00536928
                        case 0x0D_03: // 0053698a
                        case 0x0D_05: // 00536a60
                        case 0x0D_06: // 00536ae8
                        case 0x0D_07: // 00536b83
                        case 0x0D_09: // 00536bea
                        case 0x0D_0A: // 00536c6c
                        case 0x0D_0B: // 00536cce
                        case 0x0D_0C: // 00536d53
                        case 0x0D_0D: // 00536dc8
                        case 0x0D_0E: // 00536e6e
                        case 0x0D_0F: // 00536ee8
                        case 0x0D_10: // 00536f73
                        case 0x0D_11: // 00536fe8
                        case 0x0D_12: // 0053705c
                        case 0x0D_13: // 005370be // get pet information?
                        case 0x0E_01: // 0054ddb4
                        case 0x0E_02: //
                        case 0x0E_03: //
                        case 0x0E_04: //
                        case 0x0E_05: //
                        case 0x0E_06: //
                        case 0x0E_07: //
                        case 0x0E_09: //
                        case 0x0E_0A: //
                        case 0x0E_0B: //
                        case 0x0E_14: //

                        case 0x0F_02: // 00511e18
                        case 0x0F_03: // 00511e8c
                        case 0x0F_04: // 00511f75
                        case 0x0F_05: // 00512053
                        case 0x0F_06: // 005120e6
                        case 0x0F_07: // 00512176
                        case 0x0F_09: // 005121da
                        case 0x0F_0A: // 00512236
                        case 0x0F_0B: // 005122a4
                        case 0x0F_0C: // 0051239d
                        case 0x0F_0D: // 0051242c
                        case 0x0F_0E: // 005124f7

                        case 0x10_01: //

                        case 0x11_01: // 0059b6b4

                        case 0x12_01: // 0052807b
                        case 0x12_02: // 005280f6

                        case 0x14_01: //

                        case 0x15_01: //
                        case 0x15_02: //
                        case 0x15_03: //
                        case 0x15_04: //

                        case 0x16_01: //
                        case 0x16_02: // start tutorial?

                        case 0x17_01: // 0053a183
                        case 0x17_02: //
                        case 0x17_03: //
                        case 0x17_04: //
                        case 0x17_05: //
                        case 0x17_06: //

                        case 0x19_01: //
                        case 0x19_02: //
                        case 0x19_03: //

                        case 0x1C_01: //
                        case 0x1C_02: //
                        case 0x1C_03: //
                        case 0x1C_04: //
                        case 0x1C_0A: //

                        case 0x1F_01: //
                        case 0x1F_02: //
                        case 0x1F_03: //
                        case 0x1F_04: //
                        case 0x1F_06: //
                        case 0x1F_07: //
                        case 0x1F_0B: //

                        case 0x20_03: //
                        case 0x20_04: //

                        case 0x21_03: // 00538ce8

                        case 0x22_03: //
                        case 0x22_04: //
                        case 0x22_05: //
                        case 0x22_06: //

                        case 0x23_01: // 005a19da
                        */

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
                    } catch when(client.Token.IsCancellationRequested) {
                        // ignore canceled
                    } catch(Exception e) {
                        client.Logger.LogError(e, null);
                    }

                    client.TcpClient.Close();

                    if(client.Username != null) {
                        Database.LogOut(client.Username);
                        client.Logger.LogInformation($"Player {client.Username} disconnected");
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

            quests = Quest.Load(SeanArchive.Extract("./c_quest_eng.sdb"));
            var archive = SeanArchive.Extract("./client_table_eng.sdb");

            teleporters = Teleport  .Load(archive.First(x => x.Name == "teleport_list.txt"));
            var npcs    = NPCName   .Load(archive.First(x => x.Name == "npc_list.txt"));
            resources   = Extractor.Resource.Load(archive.First(x => x.Name == "res_list.txt"));
            lootTables  = ResCounter.Load(archive.First(x => x.Name == "res_counter.txt"));
            items       = ItemAtt   .Load(archive.First(x => x.Name == "item_att.txt"));
            equipment   = EquAtt    .Load(archive.First(x => x.Name == "equ_att.txt"));
            skills      = SkillInfo .Load(archive.First(x => x.Name == "skill_exp.txt"));

            var mapList = MapList.Load(archive.First(x => x.Name == "map_list.txt"));

            var dialogs = JsonSerializer.Deserialize<JsonElement[]>(File.ReadAllText("./dialog_data.json"));

            dialogData = new Dictionary<int, DialogData[]>();
            foreach(var npc in npcs) {
                var d = new List<DialogData>();

                foreach(var dialog in dialogs) {
                    if(dialog.GetProperty("Npc").EnumerateArray().All(x => x.GetInt32() != npc.Id)) {
                        continue;
                    }

                    if(!dialog.TryGetProperty("Previous quests", out var prev) || prev.ValueKind != JsonValueKind.Number) {
                        continue;
                    }

                    int quest;
                    bool begins;
                    if(dialog.TryGetProperty("Start", out var start) && start.ValueKind == JsonValueKind.Number) {
                        quest = start.GetInt32();
                        begins = true;
                    } else if(dialog.TryGetProperty("End", out var end) && end.ValueKind == JsonValueKind.Number) {
                        quest = end.GetInt32();
                        begins = false;
                    } else {
                        continue;
                    }

                    d.Add(new DialogData {
                        Id = dialog.GetProperty("Dialog").GetInt32(),
                        Quest = quest,
                        Begins = begins,
                        Previous = prev.GetInt32()
                    });
                    // if(dialog["Previous quests"])
                }

                dialogData[npc.Id] = d.ToArray();
            }

            // bundle all entities together to make lookup easier
            maps = new MapData[mapList.Length];
            for(int i = 0; i < mapList.Length; i++) {
                var item = mapList[i];
                if(item.Name == null)
                    continue;

                maps[i] = new MapData {
                    Npcs = npcs.Where(x => x.MapId == i).ToArray(),
                    Resources = resources.Where(x => x.MapId == i).ToArray(),
                    Teleporters = teleporters.Where(x => x.FromMap == i).ToArray()
                };
            }

            Debug.Assert(teleporters.All(x => x.FromMap == 0 || maps[x.FromMap] != null)); // all teleporters registered
            Debug.Assert(npcs.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all teleporters registered
            Debug.Assert(resources.All(x => x.MapId == 0 || maps[x.MapId] != null)); // all teleporters registered

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
