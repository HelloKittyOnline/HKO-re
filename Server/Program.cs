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
        internal static ProdRule[] prodRules;

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
                    } catch when(client.Token.IsCancellationRequested) {
                        // ignore canceled
                    } catch(Exception e) {
                        client.Logger.LogError(e, null);
                    }

                    client.TcpClient.Close();

                    if(client.Username != null) {
                        Database.LogOut(client.Username, client.Player);
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
            prodRules   = ProdRule  .Load(archive.First(x => x.Name == "prod_rule.txt"));

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
