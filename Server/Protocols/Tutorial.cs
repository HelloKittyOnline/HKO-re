using System;

namespace Server.Protocols;

static class Tutorial {
    [Request(0x16, 0x01)] // 0054c11c
    static void Step(Client client) {
        // 01-00-00-00
        var action = client.ReadByte();
        // 0 = prev
        // 1 = next

        var step = client.ReadInt32();

        if(action == 0) {
            // TODO: back button
            return;
        }

        switch(client.Player.CurrentMap, step) {
            case (1, 1):
                Send01(client, 1, 2, 1, true, true, false);
                Send02(client, 1, 443, 473);
                break;
            case (1, 2):
                Send01(client, 1, 3, 1, false, false, false);
                Send03(client, 270);
                break;
            case (1, 3):
                Send01(client, 1, 4, 1, true, true, false);
                Send02(client, 1, 698, 372);
                break;
            case (2, 1):
                Send01(client, 2, 2, 1, true, true, false);
                Send02(client, 0, 0, 0);
                break;
            case (2, 2):
                Send01(client, 2, 3, 1, false, false, false);
                break;
            case (2, 3):
                Send01(client, 2, 4, 1, true, true, true);
                break;
            case (2, 4):
                if(!client.Player.QuestFlags.ContainsKey(99)) {
                    Send01(client, 2, 4, 2, true, true, false);
                } else {
                    Send01(client, 2, 5, 1, true, true, false);
                }
                break;
            case (2, 5):
                Send01(client, 2, 6, 1, true, true, false);
                break;
            case (2, 6):
                Send01(client, 2, 7, 1, true, true, false);
                break;
            case (2, 7):
                Send01(client, 2, 8, 1, true, true, false);
                break;
            case (2, 8):
                Send01(client, 2, 9, 1, false, true, false);
                break;
            case (2, 9):
                Send01(client, 2, 10, 1, true, true, false);
                break;
            case (2, 10):
                Send01(client, 2, 11, 1, true, false, false);
                Send02(client, 1, 733, 319);
                break;
            case (3, 1):
                Send01(client, 3, 2, 1, true, true, false);
                break;
            case (3, 2):
                Send01(client, 3, 3, 1, true, true, false);
                break;
            case (3, 3):
                Send01(client, 3, 4, 1, true, true, true);
                client.Player.TutorialState = 0;
                break;
            case (3, 4):
                if(client.Player.TutorialState == 0) {
                    Send01(client, 3, 4, 2, true, true, false);
                    client.Player.TutorialState = 1;
                } else {
                    Send01(client, 3, 5, 1, true, true, false);
                    Send02(client, 1, 707, 332);
                }
                break;
            case (50007, 1):
                Send01(client, 4, 2, 1, true, true, false);
                break;
            case (50007, 2):
                Send01(client, 4, 3, 1, true, true, false);
                break;
            case (50007, 3):
                Send01(client, 4, 4, 1, true, true, false);
                break;
            case (50007, 4):
                Send01(client, 4, 5, 1, false, false, false);
                break;
            case (50007, 5):
                Send01(client, 4, 6, 1, false, false, false);
                break;
            case (50007, 6):
                Send01(client, 4, 7, 1, false, false, false);
                break;
            case (50007, 7):
                Send01(client, 4, 8, 1, true, true, false);
                break;
            case (50007, 8):
                Send01(client, 4, 9, 1, false, false, false);
                break;
            case (50007, 9):
                Send01(client, 4, 10, 1, false, false, false);
                break;
            case (50007, 10):
                Send01(client, 4, 11, 1, true, false, false);
                // TODO: spawn mob
                break;
            case (50007, 11):
                Send01(client, 4, 12, 1, true, true, true);
                break;
            case (50007, 12):
                Send01(client, 4, 13, 1, true, true, false);
                break;
            case (50007, 13):
                Send01(client, 4, 14, 1, true, true, false);
                break;
            case (50007, 14):
                Send01(client, 4, 15, 1, true, true, false);
                Send02(client, 1, 871, 418);
                break;
        }
    }

    [Request(0x16, 0x02)] // 0054c1a8 // start tutorial?
    public static void Recv02(Client client) {
        throw new NotImplementedException();
    }

    public static void Send01(Client client, int a, int b, int c, bool showText, bool showPrev, bool showNext) {
        var _b = new PacketBuilder(0x16, 0x01);

        _b.WriteInt(a); // id 1
        _b.WriteInt(b); // id 2
        _b.WriteInt(c); // id 3

        _b.WriteInt(0);

        _b.WriteByte((byte)(showText ? 1 : 0));
        _b.WriteByte((byte)(showPrev ? 1 : 0));
        _b.WriteByte((byte)(showNext ? 1 : 0));

        _b.Send(client);
    }

    public static void Send02(Client client, byte v1, int x, int y) {
        var _b = new PacketBuilder(0x16, 0x02);

        _b.WriteByte(v1);
        _b.WriteInt(x);
        _b.WriteInt(y);

        _b.Send(client);
    }

    static void Send03(Client client, int npcId) {
        var _b = new PacketBuilder(0x16, 0x03);

        _b.WriteInt(npcId);

        _b.Send(client);
    }
}
