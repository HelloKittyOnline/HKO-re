using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    class Program {
        // 1   | "Dream Room 1"
        // 2   | "Dream Room 2"
        // 3   | "Dream Room 3"
        // 4   | "Dream Room 4"
        // 5   | "Dream Room 5"
        // 6   | "Dream Room 6"
        // 7   | "Dream Room 7"
        // 8   | "Sanrio Harbour"          | p2
        // 9   | "Florapolis"              | p3
        // 10  | "London"                  | p7
        // 11  | "Paris"                   | p5
        // 12  | "New York"                | p4
        // 13  | nothing
        // 14  | "Tokyo"                   | p9
        // 15  | "Dream Carnival"
        // 16  | "02"
        // 17  | "Big Ben"
        // 18  | "Buckingham Palace"
        // 25  | "Chatting Room"
        // 50  | "West Stars Plain"        | p5
        // 75  | "Forbidden Museum"        | p6
        // 100 | "Mt. Fujiyama"
        // 150 | "eam Room 4"
        // 200 | "Room 5"
        // 300 | "TEST South Dream Forest" | p3
        // 400 | nothing
        // 10000 - 20000 crash
        // 30000 - 50000 Farm (f2)
        // 60000 - ??    nothing
        private static int MapId = 14;

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
        static void Send00_02_XX(Stream clientStream, byte x) {
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


        static void writeInvItem(BinaryWriter w) {
            w.Write((int)0); // id
            w.Write((int)0);
            w.Write((int)0);
        }
        static void writeFriend(BinaryWriter w) {
            // name - wchar[32]
            for(int i = 0; i < 32; i++)
                w.Write((short)0);
            w.Write((int)0); // length
        }
        static void writePetData(BinaryWriter w) {
            for(int i = 0; i < 0xd8; i++)
                w.Write((byte)0);
        }
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

                b.Add((byte)1); // gender (1 = male, else female)

                var bytes = new byte[0x8c10 - 0x542C];
                {
                    var s = new MemoryStream(bytes);
                    var w = new BinaryWriter(s);

                    // starts at 0x542C
                    w.Write((int)9001); // body
                    w.Write((int)0);
                    w.Write((int)18301); // face
                    w.Write((int)15802); // shoes
                    w.Write((int)14501); // pants
                    w.Write((int)13001); // clothes
                    w.Write((int)9051);  // hair
                    for(int i = 7; i < 18; i++) {
                        w.Write((int)0);
                    }

                    w.Write((int)123456); // money

                    w.Write((byte)0); // status (0 = online, 1 = busy, 2 = away)
                    w.Write((byte)0); // petId
                    w.Write((byte)0); // emotionSomething
                    w.Write((byte)0); // unused
                    w.Write((byte)1); // blood type
                    w.Write((byte)1); // birth month
                    w.Write((byte)1); // birth day
                    w.Write((byte)1); // constellation

                    w.Write((int)0); // guild id?

                    for(int i = 0; i < 10; i++)
                        w.Write((int)0); // quick bar

                    for(int i = 0; i < 76; i++)
                        w.Write((byte)0); // idk

                    for(int i = 0; i < 14; i++)
                        writeInvItem(w); // inv1
                    for(int i = 0; i < 6; i++)
                        writeInvItem(w); // inv2
                    for(int i = 0; i < 50; i++)
                        writeInvItem(w); // inv3
                    w.Write((byte)0); // inv3 size
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((byte)0);
                    for(int i = 0; i < 200; i++)
                        writeInvItem(w); // inv4
                    w.Write((byte)0); // inv4 size
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((byte)0);

                    for(int i = 0; i < 100; i++)
                        writeFriend(w); // friend list
                    w.Write((byte)0); // friend count
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((byte)0);

                    for(int i = 0; i < 50; i++)
                        writeFriend(w); // ban list
                    w.Write((byte)0); // ban count
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((byte)0);

                    for(int i = 0; i < 3; i++)
                        writePetData(w); // pet data
                }
                b.EncodeCrazy(bytes); // ((*global_gameData)->data).ItemAttEntityIds

                b.Add((short)0); // ((*global_gameData)->data).field_0x5410

                // map id
                b.Add((int)MapId);

                var stats = new byte[60];
                {
                    var s = new MemoryStream(stats);
                    var w = new BinaryWriter(s);

                    // starts at 0x911C
                    w.Write((int)1); // overall level
                    w.Write((int)0); // level progress

                    w.Write((byte)0); // ???
                    w.Write((byte)0); // ???
                    w.Write((byte)0); // ???
                    w.Write((byte)0); // unused?

                    w.Write((short)1); // Planting
                    w.Write((short)2); // Mining
                    w.Write((short)3); // Woodcutting
                    w.Write((short)4); // Gathering
                    w.Write((short)5); // Forging
                    w.Write((short)6); // Carpentry
                    w.Write((short)7); // Cooking
                    w.Write((short)8); // Tailoring

                    w.Write((int)0); // Planting    progress
                    w.Write((int)0); // Mining      progress
                    w.Write((int)0); // Woodcutting progress
                    w.Write((int)0); // Gathering   progress
                    w.Write((int)0); // Forging     progress
                    w.Write((int)0); // Carpentry   progress
                    w.Write((int)0); // Cooking     progress
                    w.Write((int)0); // Tailoring   progress
                }
                b.EncodeCrazy(stats);
            }

            b.Send(clientStream);
        }

        // (2, 1)
        static void Send02_01(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x1); // second switch

            b.EncodeCrazy(Array.Empty<byte>());

            b.Send(clientStream);
        }

        // (2, 9)
        static void Send02_09(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x9); // second switch

            b.Add((int)MapId); // map id
            b.Add((short)0);
            b.Add((short)0);
            b.Add((byte)0);

            /*if(mapType == 3) {
                b.EncodeCrazy(Array.Empty<byte>());
                b.Add((int)0);
                b.AddString("", 1);
                b.Add((byte)0);
                b.Add((byte)0);
                b.EncodeCrazy(Array.Empty<byte>());
                b.Add((int)0);
            } else if(mapType == 4) {
                b.EncodeCrazy(Array.Empty<byte>());
                b.EncodeCrazy(Array.Empty<byte>());
            }*/

            b.Add((byte)0);
            /*
            if(byte == 99) {
                // have_data
                b.Add((int)0);
                b.AddString("", 2);
            } else {
                // no_data
            }
            */

            b.Send(clientStream);
        }

        // (2, 6E)
        static void Send02_6E(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch

            b.AddWstring("");
            b.Add((int)MapId); // map id?
            b.AddString("", 1);

            b.Send(clientStream);
        }

        // (2, 6F)
        static void Send02_6F(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch

            b.Add((byte)0);

            b.Send(clientStream);
        }

        // (5, 14)
        static void Send05_14(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x05); // first switch
            b.Add((byte)0x14); // second switch

            b.Add((byte)0x01);

            b.AddString("https://google.de", 1);

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
            // data.length == 124

            var name = Encoding.Unicode.GetString(data[..64]);
            // cut of null terminated
            name = name[..name.IndexOf((char)0)];
            Console.WriteLine($"{name}");

            var gender = data[64]; // 1 = male, 2 = female
            var bloodType = data[65]; // 1 = O, 2 = A, 3 = B, 4 = AB
            var birthMonth = data[66];
            var birthDay = data[67];
            var idk5 = new int[14];

            // idk5[0] = skin id 
            // idk5[2] = eye id
            // idk5[6] = hair id
            Buffer.BlockCopy(data, 68, idk5, 0, 14 * 4);
            for(int i = 0; i < 14; i++) {
                Console.WriteLine(idk5[i]);
            }

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
                // Console.WriteLine($"{a} : {b}");
            }

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
#if DEBUG
                    Console.WriteLine($"S <- C: {data[0]:X2}:");
#endif
                    /*if (data[0] == 0x7E) {
                        // 005551be
                    } else if (data[0] == 0x7F) {
                        // 005bb8a9
                    }*/
                    continue;
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
                    case 0x00_01: // 0059af3e // Auth
                        AcceptClient(r, clientStream);
                        break;
                    case 0x00_03: // 0059afd7 // after user selected world
                        SelectServer(r, clientStream);
                        break;
                    case 0x00_04: // 0059b08f // list of languages? sent after lobbyServer
                        ServerList(r, clientStream);
                        break;
                    case 0x00_0B: // 0059b14a // source location 0059b14a // sent after realmServer
                        Recieve_00_0B(r, clientStream);
                        break;
                    case 0x00_10: // 0059b1ae // has something to do with T_LOADScreen // finished loading?
                        break;
                    case 0x00_63: // 0059b253
                        Ping(r, clientStream);
                        break;

                    case 0x01_01: // 00566b0d // sent after character creation
                        CreateCharacter(r, clientStream);
                        break;
                    case 0x01_02: // 00566b72
                        SendCharacterData(clientStream, true);
                        break;
                    /*
                    case 0x01_03: // 00566bce // Delete character
                    case 0x01_05: // 00566c47 // check character name
                        // r.ReadInt16();
                        // rest wstring
                        break;*/

                    case 0x02_01: // 005defa2
                        Send00_11(clientStream);
                        Send02_09(clientStream);
                        break;
                    case 0x02_02: // 005df036 // sent after 02_09
                        break;
                    /*
                    case 0x02_04: // 005df0cb
                    case 0x02_05: // 005df144 // open web form // maybe html request?
                    case 0x02_06: // 005df1ca
                    case 0x02_07: // 005df240
                    case 0x02_08: // 005df2b4
                    case 0x02_0A: // 005df368 // send teleport
                    */
                    case 0x02_0B: { // 005df415
                            var mapId = r.ReadInt32();
                            var hashHex = r.ReadBytes(32);
                            break;
                        }
                    /*
                    case 0x02_0C: // 005df48c
                    case 0x02_0D: // 005df50c
                    case 0x02_0E: // 005df580
                    case 0x02_13: // 005df5e2
                    */
                    case 0x02_1A: { // 005df655 // sent after 02_09
                            var winmTime = r.ReadInt32();
                            break;
                        }
                    /*
                    case 0x02_1f: // 005df6e3
                    case 0x02_20: // 005df763
                    case 0x02_21: // 005df7d8
                    case 0x02_28: // 005df86e
                    case 0x02_29: // 005df8e4
                    case 0x02_2A: // 005df946
                    case 0x02_2B: // 005df9cb
                    case 0x02_2C: // 005dfa40
                    case 0x02_2D: // 005dfab4
                    */
                    case 0x02_32: // 005dfb8c //  client version information
                        Recieve_02_32(r, clientStream);
                        break;
                    /*
                    case 0x02_33: // 005dfc04
                    case 0x02_34: // 005dfc78
                    case 0x02_63: // 005dfcee
 
                    case 0x03_01: // 005d2eec // map channel message
                    case 0x03_02: // 005d2fa6
                    case 0x03_05: // 005d3044 // normal channel message
                    case 0x03_06: // 005d30dc // trade channel message
                    case 0x03_07: // 005d3174
                    case 0x03_08: // 005d320c // advice channel message
                    case 0x03_0B: // 005d3288 change chat filter
                    case 0x03_0C: // 005d331e
                    case 0x03_0D: // 005d33a7 open private message

                    case 0x04_01: // 0051afb7 // add friend
                    case 0x04_02: // 0051b056 // mail
                    case 0x04_03: // 0051b15e // delete friend
                    case 0x04_04: // 0051b1d4 // set status message // 1 byte, 0 = avalible, 1 = busy, 2 = away
                    case 0x04_05: // 0051b253 // add player to blacklist
                    case 0x04_07: // 0051b31c // remove player from blacklist

                    case 0x05_01: // 00573de8
                    case 0x05_02: //
                    case 0x05_03: //
                    case 0x05_04: //
                    case 0x05_05: //
                    case 0x05_06: //
                    case 0x05_07: //
                    case 0x05_08: //
                    case 0x05_09: //
                    case 0x05_0A: //
                    case 0x05_0B: //
                    case 0x05_0C: //
                    case 0x05_11: //
                    case 0x05_14: //
                    case 0x05_15: //
                    case 0x05_16: //

                    case 0x06_01: // 005a2513

                    case 0x07_01: // 00529917
                    case 0x07_04: // 005299a6

                    case 0x08_01: //
                    case 0x08_02: //
                    case 0x08_03: //
                    case 0x08_04: //
                    case 0x08_06: //

                    case 0x09_01: // 00586fd2
                    case 0x09_02: // 
                    case 0x09_03: // 
                    case 0x09_06: // 
                    case 0x09_0F: // 
                    case 0x09_10: // 
                    case 0x09_11: // 
                    case 0x09_20: // check item delivery available?
                    case 0x09_21: // 
                    case 0x09_22: // 

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

                    case 0x13_01: // add player to group
                    case 0x13_02: //
                    case 0x13_03: //
                    case 0x13_04: //
                    case 0x13_05: //
                    case 0x13_06: //
                    case 0x13_07: //
                    case 0x13_08: //
                    case 0x13_09: //
                    case 0x13_0A: //
                    case 0x13_0B: //
                    case 0x13_0C: //
                    case 0x13_0D: //

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
