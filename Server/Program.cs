using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    class Program {
        #region Responses
        // (0,1)
        static void SendLobby(Stream clientStream, bool lobby) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer");

            // idk
            b.Add((short)0);

            // idk
            b.Add((short)1);
            // b.Add((byte)0);

            b.Send(clientStream);
        }

        // (0,2,1)
        static void SendAcceptClient(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)0x1); // third switch

            b.AddString("");
            b.AddString(""); // appended to username??
            b.AddString(""); // blowfish encrypted stuff???

            b.Send(clientStream);
        }

        // (0,2,3)
        static void Send00_02_03(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)0x3); // third switch

            b.AddString("01/01/9999"); // something time related

            b.Send(clientStream);
        }

        // (0,2,x) // x = 2,4,5,6,7,8,9
        static void Send00_02_02(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)x); // third switch

            b.Send(clientStream);
        }

        // (0,4)
        static void SendServerList(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x4); // second switch

            // some condition?
            b.Add((short)0);

            // server count
            b.Add(1);
            {
                b.Add(1); // server number
                b.AddWstring("Test Sevrer");

                // world count
                b.Add(1);
                {
                    b.Add(1); // wolrd number
                    b.AddWstring("Test World");
                    b.Add(0); // world status
                }
            }


            b.Send(clientStream);
        }

        // (0,5)
        static void Send00_05(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x5); // second switch

            int count = 1;
            b.Add(count);

            for(int i = 1; i <= count; i++) {
                b.Add(i); // id??
                b.AddString("Test server");
            }

            b.Send(clientStream);
        }

        // (0, 11)
        static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xB); // second switch

            b.Add(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1"); // address
            b.Add((short)12345); // port

            b.Send(clientStream);
        }

        // (0, 12, x) x = 0-7
        static void Send00_0C(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xC); // second switch

            b.Add((byte)x); // 0-7 switch

            b.Send(clientStream);
        }

        // (0, 13, x) x = 2-6
        static void Send00_0D(Stream clientStream, int x) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xD); // second switch

            b.Add((short)x); // (2-6) switch

            b.Send(clientStream);
        }

        // (0, 14)
        // almost the same as (0, 11)
        static void Send00_0E(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xE); // second switch

            b.Add(0); // some global

            // parameters for FUN_0060699c
            b.AddString("127.0.0.1");
            b.Add((short)12345);

            b.Send(clientStream);
        }

        // (0, 16)
        static void Send00_10(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x10); // second switch

            // some weird looped shit

            b.Send(clientStream);
        }

        // (0, 17)
        static void Send00_11(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x00); // first switch
            b.Add((byte)0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.Add((int)1 << 30); 

            b.Send(clientStream);
        }

        // (0, 18)
        static void Send00_12(Stream clientStream) { }

        // (0, 99)
        static void Send00_5A(Stream clientStream) { }

        // (1, 2)
        // triggers character creation
        static void SendCharacterData(Stream clientStream, bool exists) {
            var b = new PacketBuilder();

            b.Add((byte)0x1); // first switch
            b.Add((byte)0x2); // second switch

            // indicates if a character already exists
            b.Add(Convert.ToByte(exists));

            if(exists) {
                b.AddWstring("Lorem Ipsum"); // Character name
                b.Add((byte)0); // sets T_EnterGame->data.field_0x94

                b.EncodeCrazy(Array.Empty<byte>()); // ((*global_gameData)->data).ItemAttEntityIds

                b.Add((short)0); // ((*global_gameData)->data).field_0x5410

                // ((*global_gameData)->data).mapId
                // index into global_T_MapList_array
                // 0xf has something to do with tutorial
                b.Add((int)1);

                // 0     - ??    dream carnival (no map loads?)
                // 10000 - 20000 crash
                // 30000 - 50000 Farm (f2)
                // 60000 - ??    nothing

                var stats = new byte[256];

                stats[0] = 0; stats[1] = 0; stats[2] = 0; stats[3] = 1; // overall level
                // stats[4] = 0; stats[5] = 0; stats[6] = 0; stats[7] = 0; // idk
                stats[9] = 1; stats[10] = 0;  // Planting
                stats[11] = 2; stats[12] = 0; // Mining
                stats[13] = 3; stats[14] = 0; // Woodcutting
                stats[15] = 4; stats[16] = 0; // Gathering
                stats[17] = 5; stats[18] = 0; // Forging
                stats[19] = 6; stats[20] = 0; // Carpentry
                stats[21] = 7; stats[22] = 0; // Cooking
                stats[23] = 8; stats[24] = 0; // Tailoring

                b.EncodeCrazy(stats); // ((*global_gameData)->data).field_0x911c
            }

            b.Send(clientStream);
        }

        // (2, 1)
        static void Send02_01(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x1); // second switch

            /*var data = new byte[0x388C+1];
            data[0x388C] = 1;*/

            b.EncodeCrazy(Array.Empty<byte>());

            b.Send(clientStream);
        }

        // (2, 9)
        static void Send02_09(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x9); // second switch

            b.Add((int)0);
            b.Add((short)0);
            b.Add((short)0);
            b.Add((byte)0);

            // b.EncodeCrazy(Array.Empty<byte>());
            // b.EncodeCrazy(Array.Empty<byte>());

            /*for(int i = 0; i < 64; i++) {
                b.Add((byte)0);
            }*/

            b.Send(clientStream);
        }

        static void Send02_6E(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch

            b.AddWstring("");
            b.Add((int)30001); // map id?
            b.AddString("", 1);

            b.Send(clientStream); 
        }
        static void Send02_6F(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch
            
            b.Add((byte)0);

            b.Send(clientStream);
        }
        #endregion

        #region Packet handlers
        static void AcceptClient(BinaryReader req, Stream res) {
            var data = PacketBuilder.DecodeCrazy(req);

            var userName = Encoding.ASCII.GetString(data, 1, data[0]);
            var password = Encoding.ASCII.GetString(data, 0x42, data[0x41]);

            Console.WriteLine($"{userName}, {password}");

            SendAcceptClient(res);
        }

        static void ServerList(BinaryReader req, Stream res) {
            var count = req.ReadInt32();

            for(int i = 0; i < count; i++) {
                var len = req.ReadByte();
                var name = Encoding.ASCII.GetString(req.ReadBytes(len));
            }

            SendServerList(res);
        }

        static void SelectServer(BinaryReader req, Stream res) {
            int serverNum = req.ReadInt16();
            int worldNum = req.ReadInt16();

            // SendChangeServer(res);
            SendLobby(res, false);
        }

        static void Ping(BinaryReader req, Stream res) {
            int number = req.ReadInt32();
            // Console.WriteLine($"Ping {number}");
        }

        static void Recieve_00_0B(BinaryReader req, Stream res) {
            var idk1 = Encoding.ASCII.GetString(req.ReadBytes(req.ReadByte())); // "@"
            var idk2 = req.ReadInt32(); // = 0

            //Debugger.Break();
            Send00_0C(res, 1);
            // SendCharacterData(res, false);
        }

        static void CreateCharacter(BinaryReader req, Stream res) {
            req.ReadByte(); // 0

            var data = PacketBuilder.DecodeCrazy(req);

            var name = Encoding.Unicode.GetString(data);
            // cut of null terminated
            name = name[..name.IndexOf((char)0)];

            Console.WriteLine($"{name}");

            SendCharacterData(res, true);
        }

        static void Recieve_02_32(BinaryReader req, Stream res) {
            int count = req.ReadInt32();
            for(int i = 0; i < count; i++) {
                int aLen = req.ReadByte();
                var a = Encoding.ASCII.GetString(req.ReadBytes(aLen));

                int bLen = req.ReadByte();
                var b = Encoding.ASCII.GetString(req.ReadBytes(bLen));

                // name : version
                Console.WriteLine($"{a} : {b}");
            }

            // currently stuck on this
            // TODO: figure out response message
            // Send02_6E(clientStream);
        }
        #endregion

        static void listenClient(TcpClient socket, bool lobby) {
            Console.WriteLine($"Client {socket.Client.RemoteEndPoint} {socket.Client.LocalEndPoint}");

            var clientStream = socket.GetStream();
            var reader = new BinaryReader(clientStream);

            SendLobby(clientStream, lobby);

            while(true) {
                var head = reader.ReadBytes(3);
                if(head.Length == 0) {
                    break;
                }

                var data = reader.ReadBytes(reader.ReadUInt16());

                if(data.Length == 1) {
                    // source location 005bb8a9
                    // data[0] == 0x7f
                    break;
                }

                var ms = new MemoryStream(data);
                var r = new BinaryReader(ms);

                var id = (r.ReadByte() << 8) | r.ReadByte();

#if DEBUG
                if(id != 0x0063) { // skip ping
                    Console.WriteLine($"S <- C: {id >> 8:X2}_{id & 0xFF:X2}:");
                    // if(data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
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
                // S <- C: 00_10
                // S -> C: 02_09
                // S <- C: 00_10
                // S <- C: 02_02
                // S <- C: 02_1A
                // S <- C: 02_0B

                switch(id) {
                    case 0x00_01: // Auth
                        AcceptClient(r, clientStream);
                        break;
                    case 0x00_03: // after user selected world
                        SelectServer(r, clientStream);
                        break;
                    case 0x00_04: // list of languages? sent after lobbyServer
                        ServerList(r, clientStream);
                        break;
                    case 0x00_0B: // source location 0059b14a // sent after realmServer
                        Recieve_00_0B(r, clientStream);
                        break;
                    // case 0x00_10: // source location 0059b1ae // has something to do with T_LOADScreen // finished loading?
                    case 0x00_63: // source location 0059b253
                        Ping(r, clientStream);
                        break;

                    case 0x01_01: // source location 00566b0d // sent after character creation
                        CreateCharacter(r, clientStream);
                        break;
                    case 0x01_02:
                        SendCharacterData(clientStream, true);
                        break;
                    // case 0x01_03: // Delete character
                    /*case 0x01_05: // check character name
                        // r.ReadInt16();
                        // rest wstring
                        break;*/

                    case 0x02_01:
                        Send00_11(clientStream);
                        Send02_09(clientStream);
                        break;
                    case 0x02_32: // source location 005dfb8c //  client version information
                        Recieve_02_32(r, clientStream);
                        break;

                    /*
                    case 0x02_02: // source location 005df036 // sent after (2,9)
                    case 0x02_05: // opening item mall? // maybe html request?
                    case 0x02_04: // source location 005df0cb // sent after (2,9)
                    case 0x02_1A: // source location 005df655 // sent after 0x202

                    case 0x03_01: // map channel message
                    case 0x03_02:
                    case 0x03_05: // normal channel message
                    case 0x03_06: // trade channel message
                    case 0x03_08: // advice channel message
                    case 0x03_0A:
                    case 0x03_0B: // change chat filter
                    case 0x03_0D: // open private message

                    case 0x0E_14:

                    case 0x04_01: // add friend
                    case 0x04_02:
                    case 0x04_03:
                    case 0x04_04: // set status message // 1 byte, 0 = avalible, 1 = busy, 2 = away
                    case 0x04_05: // add player to blacklist
                    case 0x04_07:

                    case 0x07_01:
                    case 0x07_04:

                    case 0x09_20: // check item delivery available?

                    case 0x0C_03:
                    case 0x0C_07:
                    case 0x0C_08:
                    case 0x0C_09:

                    case 0x0D_02:
                    case 0x0D_03:
                    case 0x0D_05:
                    case 0x0D_06:
                    case 0x0D_07:
                    case 0x0D_09:
                    case 0x0D_0A:
                    case 0x0D_0B:
                    case 0x0D_0C:
                    case 0x0D_0D:
                    case 0x0D_0E:
                    case 0x0D_0F:
                    case 0x0D_10:
                    case 0x0D_11:
                    case 0x0D_12:
                    case 0x0D_13: // get pet information?

                    case 0x0F_02: // source location 00511e18
                    case 0x0F_03: // source location 00511e8c
                    case 0x0F_04: // source location 00511f75
                    case 0x0F_05: // source location 00512053
                    case 0x0F_06: // source location 005120e6
                    case 0x0F_07: // source location 00512176
                    case 0x0F_09: // source location 005121da
                    case 0x0F_0A: // source location 00512236
                    case 0x0F_0B:
                    case 0x0F_0C:
                    case 0x0F_0D:
                    case 0x0F_0E:

                    case 0x11_01: // source location 0059b6b4

                    case 0x12_01:
                    case 0x12_02:

                    case 0x13_01: // add player to group
                    */

                    default:
                        Console.WriteLine("Unknown");
                        if(data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
                        break;
                }
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
                    Console.WriteLine("Thread 1 done");
                });
            }
        }

        static void Main(string[] args) {
            Task.WaitAll(
                Task.Run(() => Server(25000, true))
            //, Task.Run(() => Server(12345, false))
            );
        }
    }
}
