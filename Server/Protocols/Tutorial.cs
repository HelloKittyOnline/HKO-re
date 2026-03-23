using System;
using System.Diagnostics;

namespace Server.Protocols;

static class Tutorial {
    public static int CreateInstance() {
        var b = Program.maps[4] as StandardMap;

        var map = new StandardMap {
            MapData = b.MapData,
            _mobs = [],
            _npcs = b._npcs,
            _resources = b._resources,
            _teleporters = b._teleporters,
            _checkpoints = b._checkpoints
        };

        // find first open instance slot
        int id = 30000 + 7;
        while(true) {
            map.Id = id;
            if(Program.maps.TryAdd(id, map)) {
                break;
            }
            id += 10;
        }

        return id;
    }

    public static void HandleEnterMap(Client client) {
        Player.SendSetTutorialState(client, 1);
        UpdateTut(client, 1);

        // clear invs
        client.Player.Inventory.AsSpan().Clear();
        client.Player.Equipment.AsSpan().Clear();
        client.Player.Tools.AsSpan().Clear();

        if(client.Player.CurrentMap == 2) { // dream room 2
            // clear demo quest
            client.Player.QuestFlags.Remove(99);
            Npc.SendDeleteQuest(client, 99);
        }
        if(client.Player.CurrentMap == 3) { // dream room 3
            client.Player.Inventory[0] = new() { Id = 15200, Count = 1 }; // Dream Room Pants
            client.Player.Tools[0] = new() { Id = 40, Count = 1 }; // Tutorial Scissors
            client.Player.Tools[1] = new() { Id = 41, Count = 1 }; // Tutorial Pickaxe
            client.Player.Tools[2] = new() { Id = 42, Count = 1 }; // Tutorial Axe
        }
        if(client.Player.MapType == 8) { // dream room 4
            client.Player.QuestFlags.Remove(100);
            Npc.SendDeleteQuest(client, 100);

            client.Player.Equipment[6] = new() { Id = 15200, Count = 1 }; // Dream Room Pants
            client.Player.Tools[0] = new() { Id = 40, Count = 1 }; // Tutorial Scissors
            client.Player.Tools[1] = new() { Id = 41, Count = 1 }; // Tutorial Pickaxe
            client.Player.Tools[2] = new() { Id = 42, Count = 1 }; // Tutorial Axe
        }

        // originally the game would send "get item x" notifications but that's dumb
        Player.SendSetItem(client, InvType.Player, 1, client.Player.Inventory);
        Player.SendSetItem(client, InvType.Equipment, 1, client.Player.Equipment);
        Player.SendSetItem(client, InvType.Tool, 1, client.Player.Tools);

        client.UpdateEquip();
    }

    public static void UpdateTut(Client client, int step) {
        Console.WriteLine($"tutorial {client.Player.CurrentMap},{step}");

        var room = client.Player.CurrentMap;
        if(client.Player.MapType == 8) {
            room = 4;
        }

        switch(room, step) {
            case (1, 1):
                SendStep(client, 1, step, 1, true, false, true);
                SendSetTarget(client, false, 0, 0);
                Send04(client, 1);
                break;
            case (1, 2):
                SendStep(client, 1, step, 1, true, true, false);
                SendSetTarget(client, true, 443, 473);
                break;
            case (1, 3):
                SendStep(client, 1, step, 1, false, false, false);
                SendSetTarget(client, false, 0, 0);
                ShowDialog(client, 270);
                break;
            case (1, 4):
                SendStep(client, 1, step, 1, true, true, false);
                SendSetTarget(client, true, 698, 372);
                break;
            case (2, 1):
                SendStep(client, 2, step, 1, true, false, false);
                SendSetTarget(client, true, 445, 404);
                Send04(client, 2);
                break;
            case (2, 2):
                SendStep(client, 2, step, 1, true, true, false);
                SendSetTarget(client, false, 0, 0);
                break;
            case (2, 3):
                SendStep(client, 2, step, 1, false, false, false);
                break;
            case (2, 4):
                SendStep(client, 2, step, 1, true, true, true);
                break;
            case (2, 5):
                if(!client.Player.QuestFlags.ContainsKey(99)) {
                    SendStep(client, 2, 4, 2, true, true, false);
                } else {
                    SendStep(client, 2, 5, 1, true, true, false);
                }
                break;
            case (2, 6):
                SendStep(client, 2, step, 1, true, true, false);
                break;
            case (2, 7):
                SendStep(client, 2, step, 1, true, true, false);
                break;
            case (2, 8):
                SendStep(client, 2, step, 1, true, true, false);
                break;
            case (2, 9):
                SendStep(client, 2, step, 1, false, true, false);
                break;
            case (2, 10):
                SendStep(client, 2, step, 1, true, true, false);
                break;
            case (2, 11):
                SendStep(client, 2, step, 1, true, false, false);
                SendSetTarget(client, true, 733, 319);
                break;
            case (3, 1):
                SendStep(client, 3, step, 1, true, false, false);
                SendSetTarget(client, false, 0, 0);
                Send04(client, 3);
                break;
            case (3, 2):
                SendStep(client, 3, step, 1, true, true, false);
                break;
            case (3, 3):
                SendStep(client, 3, step, 1, true, true, false);
                SendDoAction(client, 4);
                break;
            case (3, 4):
                SendStep(client, 3, step, 1, true, true, true);
                client.Player.DreamCarnival.TutorialState = 0;
                break;
            case (3, 5):
                if(client.Player.DreamCarnival.TutorialState == 0) {
                    SendStep(client, 3, 4, 2, true, true, false);
                    client.Player.DreamCarnival.TutorialState = 1;
                } else {
                    SendStep(client, 3, 5, 1, true, true, false);
                    SendSetTarget(client, true, 707, 332);
                }
                break;
            case (4, 1):
                SendStep(client, 4, step, 1, true, false, false);
                SendSetTarget(client, false, 0, 0);
                Send04(client, 4);
                break;
            case (4, 2):
                SendStep(client, 4, step, 1, true, true, false);
                SendSetResourceMarker(client, 0x474, false);
                SendSetResourceMarker(client, 0x475, false);
                SendSetResourceMarker(client, 0x476, true);
                break;
            case (4, 3):
                SendStep(client, 4, step, 1, true, true, false);
                SendSetResourceMarker(client, 0x474, false);
                SendSetResourceMarker(client, 0x475, true);
                SendSetResourceMarker(client, 0x476, false);
                break;
            case (4, 4):
                SendStep(client, 4, step, 1, true, true, false);
                SendSetResourceMarker(client, 0x474, false);
                SendSetResourceMarker(client, 0x475, false);
                SendSetResourceMarker(client, 0x476, false);
                break;
            case (4, 5):
                SendStep(client, 4, step, 1, false, false, false);
                break;
            case (4, 6):
                SendStep(client, 4, step, 1, false, false, false);
                break;
            case (4, 7):
            case (4, 8): {
                var res1 = client.GetInv(InvType.Player).GetItemCount(10205) >= 3;
                var res2 = client.GetInv(InvType.Player).GetItemCount(10206) >= 3;
                var res3 = client.GetInv(InvType.Player).GetItemCount(10211) >= 3;

                SendSetResourceMarker(client, 0x474, !res1);
                SendSetResourceMarker(client, 0x475, !res3);
                SendSetResourceMarker(client, 0x476, !res2);

                if(res1 && res2 && res3) {
                    SendStep(client, 4, 8, 1, true, true, false);
                } else {
                    SendStep(client, 4, 7, 1, false, false, false);
                }
                break;
            }
            case (4, 9):
                SendStep(client, 4, step, 1, false, false, false);
                break;
            case (4, 10):
                SendStep(client, 4, step, 1, false, false, false);
                break;
            case (4, 11): {
                SendStep(client, 4, step, 1, true, false, false);

                var mob = new MobData(1, 231, 625, 320);
                var map = client.Player.Map as StandardMap;
                map._mobs = [mob];
                Battle.SendMobs(client, [new(1, 231, 625, 320)]); // reset mob?
                break;
            }
            case (4, 12):
                SendStep(client, 4, step, 1, true, true, true);
                break;
            case (4, 13):
                SendStep(client, 4, step, 1, true, true, false);
                break;
            case (4, 14):
                SendStep(client, 4, step, 1, true, true, false);
                break;
            case (4, 15):
                SendStep(client, 4, step, 1, true, true, false);
                SendSetTarget(client, true, 871, 418);
                client.Player.Dreams.Add(17); // completed dream room
                break;
        }
    }

    [Request(0x16, 0x01)] // 0054c11c
    static void Step(ref Req req, Client client) {
        // 01-00-00-00
        var action = req.ReadByte();
        // 0 = prev
        // 1 = next

        var currentStep = req.ReadInt32();

        if(action == 0) {
            if(currentStep > 1)
                UpdateTut(client, currentStep - 1); // todo: some back actions might be buggy
        } else {
            UpdateTut(client, currentStep + 1);
        }
    }

    [Request(0x16, 0x02)] // 0054c1a8 // start tutorial?
    static void Recv02(ref Req req, Client client) {
        lock(client.Lock) {
            if(client.Player.CurrentMap != 15) // dream carnival
                return;

            Debug.Assert(client.Player.DreamCarnival.invCache == null);
            client.Player.DreamCarnival.invCache = client.Player.Inventory;
            client.Player.DreamCarnival.equCache = client.Player.Equipment;
            client.Player.DreamCarnival.toolCache = client.Player.Tools;

            client.Player.Inventory = new InventoryItem[50];
            client.Player.Equipment = new InventoryItem[14];
            client.Player.Tools = new InventoryItem[3];

            // no need to send inv update since it's done in HandleEnterMap
            Player.ChangeMap(client, 1, 352, 688);
        }
    }

    // 16_01
    public static void SendStep(Client client, int dreamRoom, int step, int sub_step, bool showText, bool showPrev, bool showNext) {
        var b = new PacketBuilder(0x16, 0x01);

        b.WriteInt(dreamRoom); // id 1
        b.WriteInt(step); // id 2
        b.WriteInt(sub_step); // id 3

        b.WriteInt(0);

        b.WriteByte(showText);
        b.WriteByte(showPrev && false); // todo: allow going back
        b.WriteByte(showNext);

        b.Send(client);
    }

    // 16_02
    static void SendSetTarget(Client client, bool show, int x, int y) {
        var _b = new PacketBuilder(0x16, 0x02);

        _b.WriteByte(show);
        _b.WriteInt(x);
        _b.WriteInt(y);

        _b.Send(client);
    }

    // 16_03
    static void ShowDialog(Client client, int npcId) {
        var b = new PacketBuilder(0x16, 0x03);

        b.WriteInt(npcId);

        b.Send(client);
    }

    // 16_04
    static void Send04(Client client, int id) {
        var b = new PacketBuilder(0x16, 0x04);

        b.WriteInt(id);

        b.Send(client);
    }

    // 16_05
    static void SendDoAction(Client client, int action) {
        var b = new PacketBuilder(0x16, 0x05);

        // 4 = close inventory
        b.WriteInt(action);

        b.Send(client);
    }

    // 16_06
    static void SendSetResourceMarker(Client client, int resId, bool show) {
        var b = new PacketBuilder(0x16, 0x06);

        b.WriteInt(resId);
        b.WriteByte(show);

        b.Send(client);
    }
}
