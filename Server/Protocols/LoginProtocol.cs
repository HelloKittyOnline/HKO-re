using System;
using System.IO;
using System.Text;

namespace Server {
    class LoginProtocol {
        public static void Handle(BinaryReader req, Stream res, ref Account account) {
            switch(req.ReadByte()) {
                case 0x00_01: // 0059af3e // Auth
                    account = AcceptClient(req, res);
                    break;
                case 0x00_03: // 0059afd7 // after user selected world
                    SelectServer(req, res, account);
                    break;
                case 0x00_04: // 0059b08f // list of languages? sent after lobbyServer
                    ServerList(req, res);
                    break;
                case 0x00_0B: // 0059b14a // source location 0059b14a // sent after realmServer
                    Recieve_00_0B(req, res);
                    break;
                case 0x00_10: // 0059b1ae // has something to do with T_LOADScreen // finished loading?
                    break;
                case 0x00_63: // 0059b253
                    Ping(req, res);
                    break;

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 00_01
        static Account AcceptClient(BinaryReader req, Stream res) {
            var data = PacketBuilder.DecodeCrazy(req);

            var userName = Encoding.ASCII.GetString(data, 1, data[0]);
            var password = Encoding.UTF7.GetString(data, 0x42, data[0x41]);

            var account = Program.database.GetPlayer(userName, password);

            if(account == null) {
                SendInvalidLogin(res);
                return null;
            }
            SendAcceptClient(res);
            return account;
        }

        // 00_03
        static void SelectServer(BinaryReader req, Stream res, Account player) {
            int serverNum = req.ReadInt16();
            int worldNum = req.ReadInt16();

            // SendChangeServer(res);
            SendLobby(res, false, player.PlayerId);
        }

        // 00_04
        static void ServerList(BinaryReader req, Stream res) {
            var count = req.ReadInt32();

            for(int i = 0; i < count; i++) {
                var len = req.ReadByte();
                var name = Encoding.ASCII.GetString(req.ReadBytes(len));
            }

            SendServerList(res);
        }

        // 00_0B
        static void Recieve_00_0B(BinaryReader req, Stream res) {
            var idk1 = Encoding.ASCII.GetString(req.ReadBytes(req.ReadByte())); // "@"
            var idk2 = req.ReadInt32(); // = 0

            Send00_0C(res, 1);
            // SendCharacterData(res, false);
        }

        // 00_63
        static void Ping(BinaryReader req, Stream res) {
            int number = req.ReadInt32();
            // Console.WriteLine($"Ping {number}");
        }
        #endregion

        #region Response
        // 00_01
        public static void SendLobby(Stream clientStream, bool lobby, int playerId) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer", 1);

            b.WriteShort(0); // (*global_hko_client)->field_0xec
            b.WriteShort((short)playerId);

            b.Send(clientStream);
        }

        // 00_02_01
        static void SendAcceptClient(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x1); // third switch

            b.AddString("", 1);
            b.AddString("", 1); // appended to username??
            b.AddString("", 1); // blowfish encrypted stuff???

            b.Send(clientStream);
        }

        // 00_02_02
        static void SendInvalidLogin(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x2); // third switch

            b.Send(clientStream);
        }

        // 00_02_03
        static void SendPlayerBanned(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x3); // third switch

            b.AddString("01/01/1999", 1); // unban timeout (01/01/1999 = never)

            b.Send(clientStream);
        }

        // 00_04
        static void SendServerList(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x4); // second switch

            // some condition?
            b.WriteShort(0);

            // server count
            b.WriteInt(1);
            {
                b.WriteInt(1); // server number
                b.AddWstring("Test Sevrer");

                // world count
                b.WriteInt(1);
                {
                    b.WriteInt(1); // wolrd number
                    b.AddWstring("Test World");
                    b.WriteInt(0); // world status
                }
            }


            b.Send(clientStream);
        }

        // 00_05
        static void Send00_05(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x5); // second switch

            int count = 1;
            b.WriteInt(count);

            for(int i = 1; i <= count; i++) {
                b.WriteInt(i); // id??
                b.AddString("Test server", 1);
            }

            b.Send(clientStream);
        }

        // 00_0B
        static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xB); // second switch

            b.WriteInt(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1", 1); // address
            b.WriteShort(12345); // port

            b.Send(clientStream);
        }

        // 00_0C_x // x = 0-7
        static void Send00_0C(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xC); // second switch

            b.WriteByte(x); // 0-7 switch

            b.Send(clientStream);
        }

        // 00_0D_x // x = 2-6
        static void Send00_0D(Stream clientStream, short x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xD); // second switch

            b.WriteShort(x); // (2-6) switch

            b.Send(clientStream);
        }

        // 00_0E
        // almost the same as 00_0B
        static void Send00_0E(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xE); // second switch

            b.WriteInt(0); // some global

            // parameters for FUN_0060699c
            b.AddString("127.0.0.1", 1);
            b.WriteShort(12345);

            b.Send(clientStream);
        }

        // 00_11
        public static void Send00_11(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x00); // first switch
            b.WriteByte(0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.WriteInt(1 << 16);

            b.Send(clientStream);
        }
        #endregion
    }
}