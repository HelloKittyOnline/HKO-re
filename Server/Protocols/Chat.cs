using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Server.Protocols;

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
    #region Request
    [Request(0x03, 0x01)] // 005d2eec // map channel message
    static void ReceiveMapChannel(ref Req req, Client client) {
        var msg = req.ReadWString();
        Logging.LogChat(client, ChatFlags.Map, msg);
        if(Commands.HandleChat(client, msg))
            return;
        SendMapChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Map) != 0), client, msg);
    }

    [Request(0x03, 0x02)] // 005d2fa6 // private message
    static void ReceivePrivateMessage(ref Req req, Client client) {
        var username = req.ReadWString();
        var message = req.ReadWString();

        var other = Program.clients.FirstOrDefault(x => x.Player.Name == username);
        if(other == null || !other.InGame)
            return;

        Logging.LogChat(client, other, message);
        SendPrivateMessage(client, other, client.Player.Name, message);
        if((other.Player.ChatFlags & ChatFlags.Private) != 0)
            SendPrivateMessage(other, client, client.Player.Name, message);
    }

    [Request(0x03, 0x05)] // 005d3044 // normal channel message
    static void ReceiveNormalChannel(ref Req req, Client client) {
        var msg = req.ReadWString();
        Logging.LogChat(client, ChatFlags.Local, msg);
        if(Commands.HandleChat(client, msg))
            return;
        SendNormalChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Local) != 0), client, msg);
    }

    [Request(0x03, 0x06)] // 005d30dc // trade channel message
    static void ReceiveTradeChannel(ref Req req, Client client) {
        var msg = req.ReadWString();
        Logging.LogChat(client, ChatFlags.Trade, msg);
        if(Commands.HandleChat(client, msg))
            return;
        SendTradeChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Trade) != 0), client, msg);
    }

    [Request(0x03, 0x07)] // 005d3174
    static void Receive07(ref Req req, Client client) {
        var msg = req.ReadWString();
        throw new NotImplementedException();
    }

    [Request(0x03, 0x08)] // 005d320c // advice channel message
    static void ReceiveAdviceChannel(ref Req req, Client client) {
        var msg = req.ReadWString();
        Logging.LogChat(client, ChatFlags.Advice, msg);
        if(Commands.HandleChat(client, msg))
            return;
        SendAdviceChannel(client.Player.Map.Players.Where(x => (x.Player.ChatFlags & ChatFlags.Advice) != 0), client, msg);
    }

    [Request(0x03, 0x0B)] // 005d3288 change chat filter
    static void SetChatFilter(ref Req req, Client client) {
        client.Player.ChatFlags = (ChatFlags)req.ReadInt32();
    }

    [Request(0x03, 0x0C)] // 005d331e
    static void ReceivePrivateChatStatus(ref Req req, Client client) {
        var open = req.ReadByte() != 0;
        var username = req.ReadWString();

        // what am i supposed to do with this?

        throw new NotImplementedException();
    }

    [Request(0x03, 0x0D)] // 005d33a7 open private message
    static void ReceiveOpenPrivateMessage(ref Req req, Client client) {
        var playerName = req.ReadWString();

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
        var b = new PacketBuilder(0x03, 0x01);

        b.WriteShort(sender.Id);
        b.WriteWString(sender.Player.Name);
        b.WriteWString(msg);

        b.Send(clients);
    }

    // 03_02
    static void SendPrivateMessage(Client client, Client other, string sender, string msg) {
        var b = new PacketBuilder(0x03, 0x02);

        b.WriteWString(other.Player.Name);
        b.WriteWString(sender);
        b.WriteWString(msg);

        b.Send(client);
    }

    // 03_03
    static void SendBannerMessage(Client client, string sender, string msg, Color color) {
        var b = new PacketBuilder(0x03, 0x03);

        b.WriteWString(sender);
        b.WriteWString(msg);

        b.WriteByte(color.R);
        b.WriteByte(color.G);
        b.WriteByte(color.B);

        b.Send(client);
    }

    // 03_05
    static void SendNormalChannel(IEnumerable<Client> clients, Client sender, string msg) {
        var b = new PacketBuilder(0x03, 0x05);

        b.WriteShort(sender.Id);
        b.WriteWString(sender.Player.Name);
        b.WriteWString(msg);

        b.Send(clients);
    }

    // 03_06
    static void SendTradeChannel(IEnumerable<Client> clients, Client sender, string msg) {
        var b = new PacketBuilder(0x03, 0x06);

        b.WriteShort(sender.Id);
        b.WriteWString(sender.Player.Name);
        b.WriteWString(msg);

        b.Send(clients);
    }

    // 03_07
    static void Send07(Client client, string sender, string msg) {
        var b = new PacketBuilder(0x03, 0x07);

        b.WriteShort(0); // unused
        b.WriteWString(sender);
        b.WriteWString(msg);

        b.Send(client);
    }

    // 03_08
    static void SendAdviceChannel(IEnumerable<Client> clients, Client sender, string msg) {
        var b = new PacketBuilder(0x03, 0x08);

        b.WriteShort(sender.Id); // unused?
        b.WriteWString(sender.Player.Name);
        b.WriteWString(msg);

        b.Send(clients);
    }

    // 03_09
    static void Send09(Client client, Client sender, string msg) {
        var b = new PacketBuilder(0x03, 0x09);

        b.WriteShort(sender.Id);
        b.WriteWString(sender.Player.Name);
        b.WriteWString(msg);

        b.Send(client);
    }

    // 03_0C
    static void SendPrivChar(Client client, Client other) {
        var b = new PacketBuilder(0x03, 0x0C);

        b.WriteWString(other.Player.Name);
        b.WriteByte(0); // idk

        // b.WriteString("", 2);
        b.WriteShort(18 * 4);
        other.Player.WriteEntities(b);

        b.Send(client);
    }

    // 03_0D
    static void SendOpenPrivateMessage(Client client, Client other) {
        var b = new PacketBuilder(0x03, 0x0D);

        b.WriteWString(other.Player.Name);

        b.Send(client);
    }
    #endregion
}
