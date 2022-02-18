using System.Drawing;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    class Chat {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x1: Recieve01(client); break; // 005d2eec // map channel message
                case 0x2: Recieve02(client); break; // 005d2fa6
                case 0x5: Recieve05(client); break; // 005d3044 // normal channel message
                case 0x6: Recieve06(client); break; // 005d30dc // trade channel message
                case 0x7: Recieve07(client); break; // 005d3174
                case 0x8: Recieve08(client); break; // 005d320c // advice channel message
                case 0xB: Recieve0B(client); break; // 005d3288 change chat filter
                case 0xC: Recieve0C(client); break; // 005d331e
                case 0xD: Recieve0D(client); break; // 005d33a7 open private message
                default:
                    client.Logger.LogWarning($"Unknown Packet 03_{id}");
                    break;
            }
        }

        #region Request
        static void Recieve01(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve02(Client client) {
            var str1 = client.ReadWString();
            var str2 = client.ReadWString();
        }

        static void Recieve05(Client client) {
            var msg = client.ReadWString();

            // broadcast message
            foreach (var _client in Program.clients) {
                // if(_client == client) continue;
                Send05(_client, client, msg);
            }
        }

        static void Recieve06(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve07(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve08(Client client) {
            var msg = client.ReadWString();
        }

        static void Recieve0B(Client client) {
            var filter = client.ReadInt32();
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

        public static void Send05(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x05); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);
            
            b.Send(client);
        }
        #endregion
    }
}