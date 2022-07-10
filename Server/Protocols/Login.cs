using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Login {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x00_01: // 0059af3e // Auth
                    AcceptClient(client);
                    break;
                case 0x00_03: // 0059afd7 // after user selected world
                    SelectServer(client);
                    break;
                case 0x00_04: // 0059b08f // list of languages? sent after lobbyServer
                    ServerList(client);
                    break;
                case 0x00_0B: // 0059b14a // source location 0059b14a // sent after realmServer
                    Recieve_00_0B(client);
                    break;
                case 0x00_10: break; // 0059b1ae // finished loading?
                case 0x00_63: // 0059b253
                    Ping(client);
                    break;
                default:
                    client.Logger.LogWarning($"Unknown Packet 00_{id:X2}");
                    break;
            }
        }

        #region Request
        // 00_01
        static void AcceptClient(Client client) {
            var data = PacketBuilder.DecodeCrazy(client.Reader);

            var username = PacketBuilder.Window1252.GetString(data, 1, data[0]);
            var password = Encoding.UTF8.GetString(data, 0x42, data[0x41]);

            var res = Database.Login(username, password, out var player);

            switch(res) {
                case LoginResponse.Ok:
                    client.Logger = Program.loggerFactory.CreateLogger($"Client[\"{username}\"]");
                    client.Player = player;
                    client.Username = username;
                    SendAcceptClient(client);
                    break;
                case LoginResponse.NoUser:
                    SendInvalidLogin(client, 8);
                    client.Close();
                    break;
                case LoginResponse.InvalidPassword:
                    SendInvalidLogin(client, 2);
                    client.Close();
                    break;
                case LoginResponse.AlreadyOnline:
                    SendInvalidLogin(client, 5);
                    client.Close();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // 00_03
        static void SelectServer(Client client) {
            int serverNum = client.ReadInt16();
            int worldNum = client.ReadInt16();

            // SendChangeServer(res);
            SendLobby(client, false);
        }

        // 00_04
        static void ServerList(Client client) {
            var count = client.ReadInt32();

            for(int i = 0; i < count; i++) {
                var name = client.ReadString();
            }

            SendServerList(client);
        }

        // 00_0B
        static void Recieve_00_0B(Client client) {
            var idk1 = client.ReadString(); // "@"
            var idk2 = client.ReadInt32(); // = 0

            Send00_0C(client, 1);
            SendTimoutVal(client);
            // SendCharacterData(res, false);
        }

        // 00_63
        static void Ping(Client client) {
            int number = client.ReadInt32();
            SendPong(client, number);
        }
        #endregion

        #region Response
        // 00_01
        public static void SendLobby(Client client, bool lobby) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x1); // second switch

            b.WriteString(lobby ? "LobbyServer" : "RealmServer", 1);

            b.WriteShort(0); // (*global_hko_client)->field_0xec
            b.WriteShort(client.Id);

            b.Send(client);
        }

        // 00_02_01
        static void SendAcceptClient(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x1); // third switch

            b.WriteString("", 1);
            b.WriteString("", 1); // appended to username??
            b.WriteString("", 1); // blowfish encrypted stuff???

            b.Send(client);
        }

        // 00_02_02
        static void SendInvalidLogin(Client client, byte id) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(id); // third switch

            b.Send(client);
        }

        // 00_02_03
        static void SendPlayerBanned(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x3); // third switch

            b.WriteString("01/01/1999", 1); // unban timeout (01/01/1999 = never)

            b.Send(client);
        }

        // 00_04
        static void SendServerList(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x4); // second switch

            // some condition?
            b.WriteShort(0);

            // server count
            b.WriteInt(1);
            {
                b.WriteInt(1); // server number
                b.WriteWString("Test Sevrer");

                // world count
                b.WriteInt(1);
                {
                    b.WriteInt(1); // wolrd number
                    b.WriteWString("Test World");
                    b.WriteInt(0); // world status
                }
            }


            b.Send(client);
        }

        // 00_05
        static void Send00_05(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x5); // second switch

            int count = 1;
            b.WriteInt(count);

            for(int i = 1; i <= count; i++) {
                b.WriteInt(i); // id??
                b.WriteString("Test server", 1);
            }

            b.Send(client);
        }

        // 00_0B
        static void SendChangeServer(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xB); // second switch

            b.WriteInt(1); // sets some global var

            // address of game server?
            b.WriteString("127.0.0.1", 1); // address
            b.WriteShort(12345); // port

            b.Send(client);
        }

        // 00_0C_x // x = 0-7
        static void Send00_0C(Client client, byte x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xC); // second switch

            b.WriteByte(x); // 0-7 switch

            b.Send(client);
        }

        // 00_0D_x // x = 2-6
        static void Send00_0D(Client client, short x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xD); // second switch

            b.WriteShort(x); // (2-6) switch

            b.Send(client);
        }

        // 00_0E
        // almost the same as 00_0B
        static void Send00_0E(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xE); // second switch

            b.WriteInt(0); // some global

            // parameters for FUN_0060699c
            b.WriteString("127.0.0.1", 1);
            b.WriteShort(12345);

            b.Send(client);
        }

        // 00_11
        public static void SendTimoutVal(Client client, int ms = 65536) {
            var b = new PacketBuilder();

            b.WriteByte(0x00); // first switch
            b.WriteByte(0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.WriteInt(ms);

            b.Send(client);
        }

        // 00_63
        static void SendPong(Client client, int number) {
            var b = new PacketBuilder();

            b.WriteByte(0x00); // first switch
            b.WriteByte(0x63); // second switch

            b.WriteInt(number);

            b.Send(client);
        }
        #endregion
    }
}