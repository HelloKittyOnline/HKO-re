using System;
using System.Text;

namespace Server.Protocols;

static class Login {
    #region Request
    [Request(0x00, 0x01)] // 0059af3e
    static void AcceptClient(ref Req req, Client client) {
        var data = req.DecodeCrazy();

        var username = PacketBuilder.Window1252.GetString(data, 1, data[0]);
        var password = Encoding.UTF8.GetString(data, 0x42, data[0x41]);

        var res = Database.Login(username, password, out var player, out var discordId);

        switch(res) {
            case LoginResponse.Ok:
                Logging.Logger.Information("[{username}_{userID}] Player logged in", username, discordId);
                client.Player = player;
                client.Username = username;
                client.DiscordId = discordId;
                player?.Init(client);
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

    [Request(0x00, 0x03)] // 0059afd7 // after user selected world
    static void SelectServer(ref Req req, Client client) {
        int serverNum = req.ReadInt16();
        int worldNum = req.ReadInt16();

        // SendChangeServer(res);
        SendLobby(client, false);
    }

    [Request(0x00, 0x04)] // 0059b08f // list of languages? sent after lobbyServer
    static void ServerList(ref Req req, Client client) {
        var count = req.ReadInt32();

        for(int i = 0; i < count; i++) {
            var name = req.ReadString();
        }

        SendServerList(client);
    }

    [Request(0x00, 0x0B)] // 0059b14a // sent after realmServer
    static void Recieve_00_0B(ref Req req, Client client) {
        var idk1 = req.ReadString(); // "@"
        var idk2 = req.ReadInt32(); // = 0

        Send00_0C(client, 1);
        SendTimoutVal(client);
        // SendCharacterData(res, false);
    }

    [Request(0x00, 0x10)] // 0059b1ae finished loading?
    static void Recv10(ref Req req, Client client) {
        // TODO: what to do with this?
    }

    [Request(0x00, 0x63)] // 0059b253
    static void Ping(ref Req req, Client client) {
        client.ResetTimeout();

        int number = req.ReadInt32();
        SendPong(client, number);
    }
    #endregion

    #region Response
    // 00_01
    public static void SendLobby(Client client, bool lobby = true) {
        var b = new PacketBuilder(0x00, 0x01);

        b.WriteString(lobby ? "LobbyServer" : "RealmServer", 1);

        b.WriteShort(0); // (*global_hko_client)->field_0xec
        b.WriteShort(client.Id);

        b.Send(client);
    }

    // 00_02_01
    static void SendAcceptClient(Client client) {
        var b = new PacketBuilder(0x00, 0x02);

        b.WriteByte(0x1); // third switch

        b.WriteString("", 1);
        b.WriteString("", 1); // appended to username??
        b.WriteString("", 1); // blowfish encrypted stuff???

        b.Send(client);
    }

    // 00_02_02
    static void SendInvalidLogin(Client client, byte id) {
        var b = new PacketBuilder(0x00, 0x02);

        b.WriteByte(id); // third switch

        b.Send(client);
    }

    // 00_02_03
    static void SendPlayerBanned(Client client) {
        var b = new PacketBuilder(0x00, 0x02);

        b.WriteByte(0x3); // third switch

        b.WriteString("01/01/1999", 1); // unban timeout (01/01/1999 = never)

        b.Send(client);
    }

    // 00_04
    static void SendServerList(Client client) {
        var b = new PacketBuilder(0x00, 0x04);

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
        var b = new PacketBuilder(0x00, 0x05);

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
        var b = new PacketBuilder(0x00, 0x0B);

        b.WriteInt(1); // sets some global var

        // address of game server?
        b.WriteString("127.0.0.1", 1); // address
        b.WriteShort(12345); // port

        b.Send(client);
    }

    // 00_0C_x // x = 0-7
    static void Send00_0C(Client client, byte x) {
        var b = new PacketBuilder(0x00, 0x0C);

        b.WriteByte(x); // 0-7 switch

        b.Send(client);
    }

    // 00_0D_x // x = 2-6
    static void Send00_0D(Client client, short x) {
        var b = new PacketBuilder(0x00, 0x0D);

        b.WriteShort(x); // (2-6) switch

        b.Send(client);
    }

    // 00_0E
    // almost the same as 00_0B
    static void Send00_0E(Client client) {
        var b = new PacketBuilder(0x00, 0x0E);

        b.WriteInt(0); // some global

        // parameters for FUN_0060699c
        b.WriteString("127.0.0.1", 1);
        b.WriteShort(12345);

        b.Send(client);
    }

    // 00_11
    public static void SendTimoutVal(Client client, int ms = 65536) {
        var b = new PacketBuilder(0x00, 0x11);

        // sets some global timeout flag
        // if more ms have been passed since then game sends 0x7F and disconnects
        b.WriteInt(ms);

        b.Send(client);
    }

    // 00_63
    static void SendPong(Client client, int number) {
        var b = new PacketBuilder(0x00, 0x63);

        b.WriteInt(number);

        b.Send(client);
    }
    #endregion
}
