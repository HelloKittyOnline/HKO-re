using System.Drawing;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    enum ChatFlags {
        Normal = 0x1,
        Range = 0x2,
        Guild = 0x4,
        Trade = 0x8,
        Private = 0x10,
        System = 0x20,
        Help = 0x40,
    }

    static class Chat {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x1: RecieveMapChannel(client); break; // 005d2eec // map channel message
                case 0x2: Recieve02(client); break; // 005d2fa6
                case 0x5: RecieveNormalChannel(client); break; // 005d3044 // normal channel message
                case 0x6: RecieveTradeChannel(client); break; // 005d30dc // trade channel message
                case 0x7: Recieve07(client); break; // 005d3174
                case 0x8: RecieveAdviceChannel(client); break; // 005d320c // advice channel message
                case 0xB: SetChatFilter(client); break; // 005d3288 change chat filter
                case 0xC: Recieve0C(client); break; // 005d331e
                case 0xD: Recieve0D(client); break; // 005d33a7 open private message
                default:
                    client.Logger.LogWarning($"Unknown Packet 03_{id:X2}");
                    break;
            }
        }

        #region Request
        static void RecieveMapChannel(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve02(Client client) {
            var str1 = client.ReadWString();
            var str2 = client.ReadWString();
        }

        static void RecieveNormalChannel(Client client) {
            var msg = client.ReadWString();

            // broadcast message
            foreach(var _client in Program.clients) {
                // if(_client == client) continue;
                SendNormalMessage(_client, client, msg);
            }
        }

        static void RecieveTradeChannel(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve07(Client client) {
            var msg = client.ReadWString();
        }

        static void RecieveAdviceChannel(Client client) {
            var msg = client.ReadWString();
        }

        static void SetChatFilter(Client client) {
            client.Player.ChatFlags = (ChatFlags)client.ReadInt32();
        }

        static void Recieve0C(Client client) {
            /*var msg = client.ReadWString();
            var msg = client.ReadWString();*/
        }

        static void Recieve0D(Client client) {
            var msg = client.ReadWString();
        }
        #endregion

        #region Response
        // 03_01
        public static void Send01(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_02
        public static void Send02(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x02); // second switch
            
            b.WriteWString("");
            b.WriteWString("");
            b.WriteWString("");

            b.Send(client);
        }

        // 03_03
        public static void Send03(Client client, string sender, string msg, Color color) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x03); // second switch

            b.WriteWString(msg);
            b.WriteWString(sender);

            b.WriteByte(color.R);
            b.WriteByte(color.G);
            b.WriteByte(color.B);

            b.Send(client);
        }

        // 03_05
        public static void SendNormalMessage(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x05); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_06
        public static void Send06(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x06); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_07
        public static void Send07(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x07); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_08
        public static void Send08(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x08); // second switch

            b.WriteShort(0); // unused?
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_09
        public static void Send09(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x09); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_0C
        public static void Send0C(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x0C); // second switch

            b.WriteWString("");
            b.WriteByte(0);
            b.WriteString("", 2);

            b.Send(client);
        }

        // 03_0D
        public static void Send0D(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x0D); // second switch

            b.WriteWString("");

            b.Send(client);
        }
        #endregion
    }
}