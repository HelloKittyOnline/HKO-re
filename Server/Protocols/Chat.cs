using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Serilog.Events;

namespace Server.Protocols {
    [Flags]
    enum ChatFlags {
        Map = 0x1,
        Local = 0x2,
        Guild = 0x4,
        Trade = 0x8,
        Private = 0x10,
        System = 0x20,
        Advice = 0x40,

        All = Map | Local | Guild | Trade | Private | System | Advice
    }

    static class Chat {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x1: ReceiveMapChannel(client); break; // 005d2eec // map channel message
                case 0x2: ReceivePrivateMessage(client); break; // 005d2fa6 // private message
                case 0x5: ReceiveNormalChannel(client); break; // 005d3044 // normal channel message
                case 0x6: ReceiveTradeChannel(client); break; // 005d30dc // trade channel message
                case 0x7: Receive07(client); break; // 005d3174
                case 0x8: ReceiveAdviceChannel(client); break; // 005d320c // advice channel message
                case 0xB: SetChatFilter(client); break; // 005d3288 change chat filter
                case 0xC: ReceivePrivateChatStatus(client); break; // 005d331e
                case 0xD: ReceiveOpenPrivateMessage(client); break; // 005d33a7 open private message
                default:
                    client.LogUnknown(0x03, id);
                    break;
            }
        }

        #region Request
        // 03_01
        static void ReceiveMapChannel(Client client) {
            var msg = client.ReadWString();
            Program.ChatLogger.Write(LogEventLevel.Information, "[Map] {mapId} {userID}:{username}: {message}", client.Player.CurrentMap, client.DiscordId, client.Username, msg);
            SendMapChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Map) != 0), client, msg);
        }

        // 03_02
        static void ReceivePrivateMessage(Client client) {
            var username = client.ReadWString();
            var message = client.ReadWString();

            var other = Program.clients.FirstOrDefault(x => x.Player.Name == username);
            if(other == null || !other.InGame)
                return;

            Program.ChatLogger.Write(LogEventLevel.Information, "[Prv] {user}:{username}->{other}->{otherUsername}: {message}", client.DiscordId, client.Username, other.DiscordId, other.Username, message);
            SendPrivateMessage(client, other, client.Player.Name, message);
            if((other.Player.ChatFlags & ChatFlags.Private) != 0)
                SendPrivateMessage(other, client, client.Player.Name, message);
        }

        // 03_05
        static void ReceiveNormalChannel(Client client) {
            var msg = client.ReadWString();
            Program.ChatLogger.Write(LogEventLevel.Information, "[Nrm] {mapId} {userID}:{username}: {message}", client.Player.CurrentMap, client.DiscordId, client.Username, msg);
            SendNormalChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Local) != 0), client, msg);
        }

        // 03_06
        static void ReceiveTradeChannel(Client client) {
            var msg = client.ReadWString();
            Program.ChatLogger.Write(LogEventLevel.Information, "[Trd] {mapId} {userID}:{username}: {message}", client.Player.CurrentMap, client.DiscordId, client.Username, msg);
            SendTradeChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Trade) != 0), client, msg);
        }

        // 03_07
        static void Receive07(Client client) {
            var msg = client.ReadWString();
        }

        // 03_08
        static void ReceiveAdviceChannel(Client client) {
            var msg = client.ReadWString();
            Program.ChatLogger.Write(LogEventLevel.Information, "[Adv] {mapId} {userID}:{username}: {message}", client.Player.CurrentMap, client.DiscordId, client.Username, msg);
            SendAdviceChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Advice) != 0), client, msg);
        }

        // 03_0B
        static void SetChatFilter(Client client) {
            client.Player.ChatFlags = (ChatFlags)client.ReadInt32();
        }

        // 03_0C
        static void ReceivePrivateChatStatus(Client client) {
            var open = client.ReadByte() != 0;
            var username = client.ReadWString();

            // what am i supposed to do with this?
        }

        // 03_0D
        static void ReceiveOpenPrivateMessage(Client client) {
            var playerName = client.ReadWString();

            var other = Program.clients.FirstOrDefault(x => x.InGame && x.Player.Name == playerName);

            if(other == null) {

            } else {
                SendOpenPrivateMessage(client, other);
            }
        }
        #endregion

        #region Response
        // 03_01
        static void SendMapChannel(IEnumerable<Client> clients, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            foreach(var client in clients) {
                b.Send(client);
            }
        }

        // 03_02
        static void SendPrivateMessage(Client client, Client other, string sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x02); // second switch

            b.WriteWString(other.Player.Name);
            b.WriteWString(sender);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_03
        static void Send03(Client client, string sender, string msg, Color color) {
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
        static void SendNormalChannel(IEnumerable<Client> clients, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x05); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            foreach(var client in clients) {
                b.Send(client);
            }
        }

        // 03_06
        static void SendTradeChannel(IEnumerable<Client> clients, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x06); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            foreach(var client in clients) {
                b.Send(client);
            }
        }

        // 03_07
        static void Send07(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x07); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_08
        static void SendAdviceChannel(IEnumerable<Client> clients, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x08); // second switch

            b.WriteShort(0); // unused?
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);


            foreach(var client in clients) {
                b.Send(client);
            }
        }

        // 03_09
        static void Send09(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x09); // second switch

            b.WriteShort(sender.Id);
            b.WriteWString(sender.Player.Name);
            b.WriteWString(msg);

            b.Send(client);
        }

        // 03_0C
        static void Send0C(Client client, Client sender, string msg) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x0C); // second switch

            b.WriteWString("");
            b.WriteByte(0);
            b.WriteString("", 2);

            b.Send(client);
        }

        // 03_0D
        static void SendOpenPrivateMessage(Client client, Client other) {
            var b = new PacketBuilder();

            b.WriteByte(0x03); // first switch
            b.WriteByte(0x0D); // second switch

            b.WriteWString(other.Player.Name);

            b.Send(client);
        }
        #endregion
    }
}