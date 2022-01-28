using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Extractor;

namespace Server {
    class MapData {
        public Teleport[] Teleporters { get; set; }
        public NPCName[] Npcs { get; set; }
        public Resource[] Resources { get; set; }
    }

    class Program {
        internal static DataBase database;

        internal static MapData[] maps;

        internal static Teleport[] teleporters;
        internal static NPCName[] npcs;
        internal static Resource[] resources;
        internal static ResCounter[] lootTables;

        static void listenClient(TcpClient socket, bool lobby) {
            Console.WriteLine($"Client {socket.Client.RemoteEndPoint} {socket.Client.LocalEndPoint}");

            var res = socket.GetStream();
            var reader = new BinaryReader(res);

            LoginProtocol.SendLobby(res, lobby, 1);

            Account account = null;

            bool running = true;
            while(running) {
                var head = reader.ReadBytes(3);
                if(head.Length == 0) {
                    break;
                }

                var data = reader.ReadBytes(reader.ReadUInt16());

                if(data.Length == 1) {
                    if(data[0] == 0x7E) { // 005551be
                    } else if(data[0] == 0x7F) { // 005bb8a9
                        // reset timeout
                        var b = new PacketBuilder();

                        b.WriteByte(0x7F);

                        b.Send(res);
                    }
                    continue;
                }

                var id = (data[0] << 8) | data[1];

                if(account == null && id != 1) {
                    // invalid data
                    break;
                }
#if DEBUG
                if(id != 0x0063) { // skip ping
                    Console.WriteLine($"S <- C: {id >> 8:X2}_{id & 0xFF:X2}");
                    if(data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
                }
#endif

                // S -> C: 00_01  : SendLobby        // lobby server
                // S <- C: 00_01  : AcceptClient     // send login details
                // S -> C: 00_02_1: SendAcceptClient // check login details
                // S <- C: 00_04  : ServerList       // request server list (after License accept)
                // S -> C: 00_04  : SendServerList   // send server list
                // S <- C: 00_03  : SelectServer
                // (optional) S -> C: 00_0B: SendChangeServer // redirect to different server for load balancing
                // S -> C: 00_01  : SendLobby        // realm server?
                // S <- C: 00_0B  : Idk
                // (optional) S -> C: 01_02: create new character
                // (optional) S <- C: 01_01: CreateCharacter
                // S -> C: 00_0C_1: create T_EnterGame
                // S <- C: 01_02  : 
                // S -> C: 01_02  : SendCharacterData
                // S <- C: 02_32  :
                // short pause loading
                // S <- C: 02_01
                // S -> C: 00_11
                // S -> C: 02_01
                // S -> C: 02_09
                // S -> C: 02_12
                // S <- C: 00_10
                // S <- C: 02_32
                // S <- C: 02_02
                // S <- C: 02_1A
                // S <- C: 02_0B

                var ms = new MemoryStream(data);
                var req = new BinaryReader(ms);

                switch(req.ReadByte()) {
                    case 0:
                        LoginProtocol.Handle(req, res, ref account);
                        if(account == null)
                            running = false;
                        break;
                    case 1:
                        IdkProtocol.Handle(req, res, account);
                        break;
                    case 2:
                        PlayerProtocol.Handle(req, res, account);
                        break;
                    case 4:
                        FriendProtocol.Handle(req, res, account);
                        break;
                    case 5:
                        NpcProtocol.Handle(req, res, account);
                        break;
                    case 6:
                        ProductionProtocol.Handle(req, res, account);
                        break;
                    case 9:
                        InventoryProtocol.Handle(req, res, account);
                        break;
                    case 0x13:
                        GroupProtocol.Handle(req, res, account);
                        break;

                    /*
                    case 0x03_01: // 005d2eec // map channel message
                    case 0x03_02: // 005d2fa6
                    case 0x03_05: // 005d3044 // normal channel message
                    case 0x03_06: // 005d30dc // trade channel message
                    case 0x03_07: // 005d3174
                    case 0x03_08: // 005d320c // advice channel message
                    case 0x03_0B: // 005d3288 change chat filter
                    case 0x03_0C: // 005d331e
                    case 0x03_0D: // 005d33a7 open private message
                    */

                    /*
                    case 0x07_01: // 00529917
                    case 0x07_04: // 005299a6

                    case 0x08_01: //
                    case 0x08_02: //
                    case 0x08_03: //
                    case 0x08_04: //
                    case 0x08_06: //
                    */
                    /*
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

                    case 0x0C_03: // 00537da8
                    case 0x0C_07: // 00537e23
                    case 0x0C_08: // 00537e98
                    case 0x0C_09: // 00537f23

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
                        Console.WriteLine("Unknown");
                        break;
                }
            }

            if(account != null) {
                Console.WriteLine($"Player {account.Username} disconnected");
            }
        }

        static void Server(int port, bool lobby) {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while(true) {
                var client = server.AcceptTcpClient();

                Task.Run(() => {
                    try {
                        listenClient(client, lobby);
                    } catch(Exception e) {
                        Console.WriteLine(e);
                    }
                });
            }
        }

        static void Main(string[] args) {
            database = DataBase.Load("./db.json");
            Task.Run(() => {
                while(true) {
                    Thread.Sleep(60 * 1000); // saved once a minute
                    // not sure about thread safety eh
                    try {
                        lock(database) {
                            database.Save("./db.json");
                        }
                        Console.WriteLine("Saved Database");
                    } catch(Exception e) {
                        Console.WriteLine(e);
                    }
                }
            });

            var archive = SeanArchive.Extract("./client_table_eng.sdb");

            teleporters = Teleport.Load(archive.First(x => x.Name == "teleport_list.txt"));
            npcs = NPCName.Load(archive.First(x => x.Name == "npc_list.txt"));
            resources = Resource.Load(archive.First(x => x.Name == "res_list.txt"));
            lootTables = ResCounter.Load(archive.First(x => x.Name == "res_counter.txt"));

            var mapList = MapList.Load(archive.First(x => x.Name == "map_list.txt"));

            // bundle all entities together to make lookup easier
            maps = new MapData[mapList.Length];
            for(int i = 0; i < mapList.Length; i++) {
                var item = mapList[i];
                if(item.Name == null)
                    continue;

                maps[i] = new MapData {
                    Npcs = npcs.Where(x => x.MapId == item.Id).ToArray(),
                    Resources = resources.Where(x => x.MapId == item.Id).ToArray(),
                    Teleporters = teleporters.Where(x => x.fromMap == item.Id).ToArray()
                };
            }

            Debug.Assert(teleporters.All(x => x.fromMap == 0 || maps[x.fromMap - 1] != null)); // all teleporters registered
            Debug.Assert(npcs.All(x => x.MapId == 0 || maps[x.MapId - 1] != null)); // all teleporters registered
            Debug.Assert(resources.All(x => x.MapId == 0 || maps[x.MapId - 1] != null)); // all teleporters registered

            Server(25000, true);
        }
    }
}
