using System;
using System.IO;

namespace Server {
    partial class Program {
        // 00_01
        public static void SendLobby(Stream clientStream, bool lobby) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer");

            b.Add((short)0); // (*global_hko_client)->field_0xec
            b.Add((short)1); // (*global_hko_client)->playerId

            b.Send(clientStream);
        }

        // 00_02_01
        public static void SendAcceptClient(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)0x1); // third switch

            b.AddString("");
            b.AddString(""); // appended to username??
            b.AddString(""); // blowfish encrypted stuff???

            b.Send(clientStream);
        }

        // 00_02_03
        public static void Send00_02_03(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)0x3); // third switch

            b.AddString("01/01/9999"); // something time related

            b.Send(clientStream);
        }

        // 00_04
        public static void SendServerList(Stream clientStream) {
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

        // 00_05
        public static void Send00_05(Stream clientStream) {
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

        // 00_0B
        public static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xB); // second switch

            b.Add(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1"); // address
            b.Add((short)12345); // port

            b.Send(clientStream);
        }

        // 00_0C_x // x = 0-7
        public static void Send00_0C(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xC); // second switch

            b.Add((byte)x); // 0-7 switch

            b.Send(clientStream);
        }

        // 00_0D_x // x = 2-6
        public static void Send00_0D(Stream clientStream, int x) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xD); // second switch

            b.Add((short)x); // (2-6) switch

            b.Send(clientStream);
        }

        // 00_0E
        // almost the same as 00_0B
        public static void Send00_0E(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xE); // second switch

            b.Add(0); // some global

            // parameters for FUN_0060699c
            b.AddString("127.0.0.1");
            b.Add((short)12345);

            b.Send(clientStream);
        }

        // 00_11
        public static void Send00_11(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x00); // first switch
            b.Add((byte)0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.Add((int)1 << 16);

            b.Send(clientStream);
        }

        public static void writeInvItem(BinaryWriter w) {
            w.Write((int)0); // id
            w.Write((int)0);
            w.Write((int)0);
        }
        public static void writeFriend(BinaryWriter w) {
            // name - wchar[32]
            for(int i = 0; i < 32; i++)
                w.Write((short)0);
            w.Write((int)0); // length
        }
        public static void writePetData(BinaryWriter w) {
            for(int i = 0; i < 0xd8; i++)
                w.Write((byte)0);
        }

        // 01_02
        // triggers character creation
        public static void SendCharacterData(Stream clientStream, bool exists) {
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
                    w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);
                    for(int i = 0; i < 200; i++)
                        writeInvItem(w); // inv4
                    w.Write((byte)0); // inv4 size
                    w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);

                    for(int i = 0; i < 100; i++)
                        writeFriend(w); // friend list
                    w.Write((byte)0); // friend count
                    w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);

                    for(int i = 0; i < 50; i++)
                        writeFriend(w); // ban list
                    w.Write((byte)0); // ban count
                    w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);

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

        // 02_01
        public static void Send02_01(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x1); // second switch

            var data = new byte[168];
            {
                var s = new MemoryStream(data);
                var w = new BinaryWriter(s);

                // 0x5384
                w.Write((int)0); // server token?
                w.Write((byte)0); // char str length
                for(int i = 0; i < 65; i++) {
                    w.Write((byte)0);
                }
                // null terminated wchar string
                for(int i = 0; i < 32; i++) {
                    w.Write((short)0);
                }

                // maybe this is not intended and i'm writing out of bounds here. can't tell

                for(int i = 0; i < 18; i++) {
                    w.Write((byte)0); // idk
                }

                w.Write((int)MapId); // mapId
                w.Write((int)0); // x
                w.Write((int)0); // y

                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((byte)1); // gender
            }

            b.EncodeCrazy(data);

            b.Send(clientStream);
        }

        // 02_02
        public static void Send02_02(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x2); // second switch

            b.Add((short)0); // count
            b.EncodeCrazy(Array.Empty<byte>()); // count * 267 bytes

            b.Send(clientStream);
        }

        // 02_09
        public static void Send02_09(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x9); // second switch

            b.Add((int)MapId); // map id
            b.Add((short)startX); // player x
            b.Add((short)startY); // player y
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

        // 02_12
        public static void Send02_12(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x12); // second switch

            b.Add((short)1); // player id


            var data = new byte[4];
            {
                var s = new MemoryStream(data);
                var w = new BinaryWriter(s);

                w.Write((int)1);
            }
            b.EncodeCrazy(data);

            b.Send(clientStream);
        }

        // 02_0F
        public static void SendTeleportPlayer(Stream clientStream, short playerId, int x, int y) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0xF); // second switch

            b.Add((short)playerId); // player id
            b.Add((int)x); // x
            b.Add((int)y); // y

            b.Send(clientStream);
        }

        // 02_6E
        public static void Send02_6E(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch

            b.AddWstring("");
            b.Add((int)MapId); // map id?
            b.AddString("", 1);

            b.Send(clientStream);
        }

        // 02_6F
        public static void Send02_6F(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x02); // first switch
            b.Add((byte)0x6E); // second switch

            b.Add((byte)0);

            b.Send(clientStream);
        }

        // 05_14
        public static void Send05_14(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x05); // first switch
            b.Add((byte)0x14); // second switch

            b.Add((byte)0x01);

            b.AddString("https://google.de", 1);

            b.Send(clientStream);
        }
    }
}
