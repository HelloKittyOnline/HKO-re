using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extractor;

namespace Server.Protocols;

static class Player {
    public static void ChangeMap(Client client) {
        var map = client.Player.Map;

        SendChangeMap(client);

        SendNpcs(client, map.Npcs);
        Npc.UpdateQuestMarkers(client, map.Npcs.Select(x => x.Id));

        SendTeleporters(client, map.Teleporters);
        SendRes(client, map.Resources);
        SendCheckpoints(client, map.Checkpoints);

        switch(client.Player.CurrentMap) {
            case 1: // Dream room 1
                Tutorial.Send01(client, 1, 1, 1, true, false, true);
                break;
            case 2: // Dream room 2
                client.Player.QuestFlags.Remove(99); // clear tutorial quest
                Npc.SendDeleteQuest(client, 99);
                Tutorial.Send01(client, 2, 1, 1, true, false, false);
                Tutorial.Send02(client, 1, 445, 404);
                break;
            case 3: // Dream room 3
                Tutorial.Send01(client, 3, 1, 1, true, false, false);
                break;
            case 50007: // Dream room 4
                client.Player.QuestFlags.Remove(100); // clear tutorial quest
                Npc.SendDeleteQuest(client, 100);
                Tutorial.Send01(client, 4, 1, 1, true, false, false);
                break;

            case 92 when client.Player.QuestFlags.GetValueOrDefault(1008, QuestStatus.None) == QuestStatus.Running: // handle map visit for quest 1008 "Check Out The View"
                client.SetQuestFlag(1008, 0);
                break;

            // handle map investigate for quest 167 "Lost in the Forest"
            case 32:
                DelayInvestigate(client, 32, 0);
                break;
            case 31:
                DelayInvestigate(client, 31, 1);
                break;
            case 33:
                DelayInvestigate(client, 33, 2);
                break;
        }

        Battle.SendMobs(client, map.Mobs);

        var others = map.Players.Where(other => other != client).ToArray();

        if(others.Length <= 0)
            return;

        var temp = new Span<Client>(ref client);
        SendAddPlayers(others, temp); // send other players to client
        SendAddPlayers(temp, others); // send client to other players
    }

    static async Task DelayInvestigate(Client client, int map, byte flag) {
        if(client.Player.QuestFlags.GetValueOrDefault(167, QuestStatus.None) != QuestStatus.Running) {
            return;
        }

        await Task.Delay(10_000);

        // check again just to make sure
        if(client.InGame && client.Player.CurrentMap == map && client.Player.QuestFlags.GetValueOrDefault(167, QuestStatus.None) == QuestStatus.Running) {
            try {
                client.SetQuestFlag(167, flag);
            } catch {
                client.Close();
            }
        }
    }

    #region Request
    [Request(0x02, 0x01)] // 005defa2
    static void EnterGame(ref Req req, Client client) {
        req.ReadByte(); // idk

        SendPlayerData(client);
        SendPlayerHpSta(client);

        // register instance maps
        Program.maps[client.Player.Farm.Id] = client.Player.Farm;
        Program.maps[client.Player.Farm.House.Floor0.Id] = client.Player.Farm.House.Floor0;
        Program.maps[client.Player.Farm.House.Floor1.Id] = client.Player.Farm.House.Floor1;
        Program.maps[client.Player.Farm.House.Floor2.Id] = client.Player.Farm.House.Floor2;
        client.InGame = true;

        ChangeMap(client);
    }

    [Request(0x02, 0x02)] // 005df036 // sent after map load
    static void Recv02(ref Req req, Client client) {
        // throw new NotImplementedException();
    }

    [Request(0x02, 0x04)] // 005df0cb
    static void OnPlayerMove(ref Req req, Client client) {
        // player walking
        var mapId = req.ReadInt32(); // mapId
        var x = req.ReadInt32(); // x
        var y = req.ReadInt32(); // y

        var player = client.Player;

        // cancel player action like harvesting
        client.CancelAction();

        player.PositionX = x;
        player.PositionY = y;

        BroadcastMovePlayer(client);
    }

    [Request(0x02, 0x05)] // 005df144
    static void SetPlayerStatus(ref Req req, Client client) {
        var data = req.ReadByte();
        // 0 = close

        client.Player.Status = data;
        BroadcastPlayerStatus(client);
    }

    [Request(0x02, 0x06)] // 005df1ca
    static void SetPlayerEmote(ref Req req, Client client) {
        var emote = req.ReadInt32();
        // 1 = blink
        // 2 = yay
        // ...
        // 26 = wave

        BroadcastPlayerEmote(client, emote);
    }

    [Request(0x02, 0x07)] // 005df240
    static void SetPlayerRotation(ref Req req, Client client) {
        var rotation = req.ReadInt16();
        // 1 = north
        // 2 = north east
        // 3 = east
        // 4 = south east
        // 5 = south
        // 6 = south west
        // 7 = west
        // 8 = north west

        client.Player.Rotation = (byte)rotation;
        SendRotatePlayer(client);
    }

    [Request(0x02, 0x08)] // 005df2b4
    static void SetPlayerState(ref Req req, Client client) {
        var state = req.ReadInt16();
        // 1 = standing
        // 3 = sitting
        // 4 = gathering

        client.Player.State = (byte)state;
        SendPlayerState(client);
    }

    [Request(0x02, 0x0A)] // 005df368
    static void TakeTeleport(ref Req req, Client client) {
        var tpId = req.ReadInt16();
        var idk = req.ReadByte(); // always 1?

        var player = client.Player;
        var oldMap = player.Map;

        if(client.Player.MapType == 3) {
            player.ReturnFromFarm();
        } else if(client.Player.MapType == 4) {
            // tp from house

            if(tpId == 352 + 10 - 1 && client.Player.CurrentMap % 10 > 0) { // to previous room
                client.Player.CurrentMap--; // next room is id - 1
                client.Player.PositionX = 570;
                client.Player.PositionY = 205;
            } else if(tpId == 352 + 10 && client.Player.CurrentMap % 10 < 3) { // to next room
                client.Player.CurrentMap++; // next room is id + 1
                client.Player.PositionX = 108 + 20;
                client.Player.PositionY = 404 + 20;
            } else { // back to farm
                var ownerId = (client.Player.CurrentMap - 30000) / 10;
                client.Player.CurrentMap = 30000 + ownerId * 10;
                client.Player.PositionX = 832;
                client.Player.PositionY = 432;
            }
        } else {
            var tp = Program.teleporters[tpId];
            if(tp.FromMap != oldMap.Id || tp.ToMap == 0)
                return;

            if(tp.KeyItem != 0 && client.GetInv(InvType.Player).GetItemCount(tp.KeyItem) < tp.KeyItemCount) {
                return; // missing key item
            }

            player.PositionX = tp.ToX;
            player.PositionY = tp.ToY;
            player.CurrentMap = tp.ToMap;
        }

        // delete players from old map
        SendDeletePlayer(oldMap.Players, client);

        ChangeMap(client);
    }

    [Request(0x02, 0x0B)] // 005df415
    static void CheckMapHash(ref Req req, Client client) {
        var mapId = req.ReadInt32();
        var hashHex = req.ReadBytes(32);
    }

    [Request(0x02, 0x0C)] // 005df48c
    static void EquipItem(ref Req req, Client client) {
        var inventorySlot = req.ReadByte();

        lock(client.Player) {
            var item = client.GetItem(InvType.Player, inventorySlot - 1);
            if(item.Id == 0)
                return;

            var att = item.Item.Data;
            if(client.Player.Levels[(int)Skill.General] < att.Level) {
                return; // level no sufficient
            }

            if(att.Type == ItemType.Tool) {
                var type = att.SubId / 1000;
                // 0 = scissors
                // 1 = pickaxe
                // 2 = axe
                // 3 = hoe - unused
                item.Swap(client.GetItem(InvType.Tool, type));
            } else if(att.Type == ItemType.Equipment) {
                var equ = Program.equipment[att.SubId];
                if(equ.Gender != 0 && equ.Gender != client.Player.Gender)
                    return;

                var type = (byte)equ.Type;
                if(0 >= type || type >= 14)
                    return;

                item.Swap(client.GetItem(InvType.Equipment, type - 1));
            }
        }
    }

    [Request(0x02, 0x0D)] // 005df50c
    static void UnEquipItem(ref Req req, Client client) {
        var slot = req.ReadByte() - 1;

        lock(client.Player) {
            client.GetItem(InvType.Equipment, slot).MoveTo(InvType.Player);
        }
    }

    [Request(0x02, 0x0E)] // 005df580
    static void UnEquipTool(ref Req req, Client client) {
        var slot = req.ReadByte() - 1;

        lock(client.Player) {
            client.GetItem(InvType.Tool, slot).MoveTo(InvType.Player);
        }
    }

    [Request(0x02, 0x13)] // 005df5e2
    private static void Recieve_02_13(ref Req req, Client client) {
        // multiple sources?
        // cancel production
        client.CancelAction();
    }

    [Request(0x02, 0x1A)] // 005df655 // sent after 02_09
    static void Recieve_02_1A(ref Req req, Client client) {
        var winmTime = req.ReadInt32();
    }

    [Request(0x02, 0x1f)] // 005df6e3 // set quickbar item
    static void SetQuickbar(ref Req req, Client client) {
        var slot = req.ReadByte();
        var itemId = req.ReadInt32();

        client.Player.Quickbar[slot - 1] = itemId;
    }

    [Request(0x02, 0x20)] // 005df763 // change player info
    static void SetPlayerInfo(ref Req req, Client client) {
        var data = req.DecodeCrazy(); // 970 bytes

        var favoriteFood = PacketBuilder.Window1252.GetString(data, 1, data[0]); // 0 - 37
        var favoriteMovie = PacketBuilder.Window1252.GetString(data, 39, data[38]); // 38 - 63
        var location = Encoding.Unicode.GetString(data, 64, 36 * 2); // 63 - 135
        var favoriteMusic = PacketBuilder.Window1252.GetString(data, 137, data[136]); // 136 - 201
        var favoritePerson = Encoding.Unicode.GetString(data, 202, 64 * 2); // 202 - 329
        var hobbies = Encoding.Unicode.GetString(data, 330, 160 * 2); // 330 - 649
        var introduction = Encoding.Unicode.GetString(data, 650, 160 * 2); // 650 - 969

        client.Player.Location = location.TrimEnd('\0');
        client.Player.FavoriteFood = favoriteFood;
        client.Player.FavoriteMovie = favoriteMovie;
        client.Player.FavoriteMusic = favoriteMusic;
        client.Player.FavoritePerson = favoritePerson.TrimEnd('\0');
        client.Player.Hobbies = hobbies.TrimEnd('\0');
        client.Player.Introduction = introduction.TrimEnd('\0');
    }

    [Request(0x02, 0x21)] // 005df7d8
    static void GetPlayerInfo(ref Req req, Client client) {
        var playerId = req.ReadInt16();
        var player = Program.clients.FirstOrDefault(x => x.Id == playerId);

        if(player != null)
            SendOtherPlayerInfo(client, player.Player);
    }

    /*
    [Request(0x02, 0x28)] // 005df86e
    [Request(0x02, 0x29)] // 005df8e4
    [Request(0x02, 0x2B)] // 005df9cb
    [Request(0x02, 0x2C)] // 005dfa40
    [Request(0x02, 0x2D)] // 005dfab4
    */
    [Request(0x02, 0x2A)] // 005df946 // sent from the same function as 0x0A why?
    static void Recv2A(ref Req req, Client client) {
        // todo: what to do with this?
    }

    [Request(0x02, 0x32)] // 005dfb8c //  client version information
    static void CheckPackageVersions(ref Req req, Client client) {
        int count = req.ReadInt32();

        /*var result = new List<string>();
        for(int i = 0; i < count; i++) {
            var name = client.ReadString();
            var version = client.ReadString();

            if(version != "v0109090007") {
                result.Add(name);
            }
        }

        for(int i = result.Count - 1; i >= 0; i--) {
            // FIXME: only send if required?
            string item = result[i];
            // SendUpdatePackage(client, 0, item);
        }
        */
    }

    /*
    [Request(0x02, 0x33)] // 005dfc04
    [Request(0x02, 0x34)] // 005dfc78
    [Request(0x02, 0x63)] // 005dfcee*/
    #endregion

    static void writeFriend(PacketBuilder w) {
        // name - wchar[32]
        for(int i = 0; i < 32; i++)
            w.WriteShort(0);
        w.WriteInt(0); // length
    }
    static void writePetData(PacketBuilder w) {
        for(int i = 0; i < 0xd8; i++)
            w.WriteByte(0);
    }

    #region Response
    // 02_01
    static void SendPlayerData(Client client) {
        var player = client.Player;

        var b = new PacketBuilder(0x02, 0x01);

        b.BeginCompress(); // player data - should be 38608 bytes
        b.WriteInt(client.Id); // something to do with farms?

        b.WritePadString("", 66); // idk
        b.WritePadWString(player.Name, 32 * 2); // null terminated wchar string

        b.Write0(18); // idk

        b.WriteInt(player.CurrentMap); // mapId
        b.WriteInt(player.PositionX); // x
        b.WriteInt(player.PositionY); // y

        b.WriteByte(player.Rotation);
        b.WriteByte(0);
        b.WriteByte(player.Speed);
        b.WriteByte(player.Gender); // gender

        player.WriteEntities(b);

        b.WriteInt(player.Money); // money

        b.WriteByte(0); // status (0 = online, 1 = busy, 2 = away)
        b.WriteByte(0); // active petId
        b.WriteByte(0); // emotionSomething
        b.WriteByte(0); // unused
        b.WriteByte(player.BloodType); // blood type
        b.WriteByte(player.BirthMonth); // birth month
        b.WriteByte(player.BirthDay); // birth day
        b.WriteByte(player.GetConstellation()); // constellation // todo: calculate this from brithday

        b.WriteInt(0); // guild id?

        for(int i = 0; i < 10; i++)
            b.WriteInt(player.Quickbar[i]); // quick bar

        b.Write0(76); // idk

        for(int i = 0; i < 14; i++)
            b.Write(player.Equipment[i]); // equipment

        for(int i = 0; i < 3; i++)
            b.Write(player.Tools[i]); // tools
        for(int i = 0; i < 3; i++)
            b.Write(new InventoryItem()); // unused tool slots

        // main inventory
        for(int i = 0; i < 50; i++)
            b.Write(player.Inventory[i]);
        b.WriteByte((byte)player.InventorySize); // size
        b.Write0(3); // unused

        // farm inventory
        for(int i = 0; i < 200; i++)
            b.Write(player.Farm.Inventory[i]);
        b.WriteByte(200); // size
        b.Write0(3); // unused

        for(int i = 0; i < 100; i++)
            writeFriend(b); // friend list
        b.WriteByte(0); // friend count
        b.Write0(3); // unused

        for(int i = 0; i < 50; i++)
            writeFriend(b); // ban list
        b.WriteByte(0); // ban count
        b.Write0(3); // unused

        for(int i = 0; i < 3; i++)
            writePetData(b); // pet data

        var questBytes = new BitVector(1000);
        foreach(var (key, val) in player.QuestFlags) {
            if(val == QuestStatus.Done) {
                questBytes[key] = true;
            }
        }
        foreach(var (key, val) in player.CheckpointFlags) {
            var data = Program.checkpoints[key];
            if(val == 1)
                questBytes[data.ActiveQuestFlag] = true;
            if(val == 2 && data.CollectedQuestFlag != 0)
                questBytes[data.CollectedQuestFlag] = true;
        }
        foreach(var val in player.Keys)
            questBytes[val] = true;
        foreach(var val in player.Dreams)
            questBytes[val] = true;
        b.Write(questBytes); // quest flags

        var currentQuests = player.QuestFlags.Where(x => x.Value == QuestStatus.Running).ToArray();
        // active quests
        for(int i = 0; i < currentQuests.Length && i < 10; i++) {
            var id = currentQuests[i].Key;
            player.QuestFlags1.TryGetValue(id, out var flag);
            b.WriteInt(id); // questId
            b.WriteByte((byte)(flag & 0xFF)); // flags1
            b.WriteByte((byte)((flag >> 8) & 0xFF)); // flags2
            b.Write0(2); // unused
        }
        b.Write0((4 + 1 + 1 + 2) * (10 - currentQuests.Length)); // remaining slots

        b.WriteByte(0);
        b.WriteByte(0); // crystals
        b.WriteByte(0);
        b.WriteByte(0);

        // 40 shorts // village friendship
        foreach(var item in player.Friendship) {
            b.WriteShort(item);
        }
        b.Write0(2 * (40 - player.Friendship.Length));

        b.Write0(128); // byte array

        player.WriteLevels(b);

        b.Write(player.ProductionFlags);
        b.Write(player.Farm);

        b.WriteInt(player.Farm.House.BuildingPermit);
        b.WriteByte(0);
        b.WriteByte(player.Farm.House.MinigameStage);
        // TODO: finish figuring out the rest

        // 0x7010

        b.Write0(0x82e0 - 0x7012);

        // 0x82e0
        b.WritePadString(player.FavoriteFood, 38);
        b.WritePadString(player.FavoriteMovie, 26);
        b.WritePadWString(player.Location, 36 * 2);
        b.WritePadString(player.FavoriteMusic, 66);
        b.WritePadWString(player.FavoritePerson, 64 * 2);
        b.WritePadWString(player.Hobbies, 160 * 2);
        b.WritePadWString(player.Introduction, 160 * 2);

        // 0x86aa
        b.Write0(0x92E0 - 0x86aa);
        // 0x92E0

        var npcFlags = new BitVector(64);
        foreach(var val in player.Npcs) {
            npcFlags[val] = true;
        }
        b.Write(npcFlags); // npc descriptions

        b.WriteInt(player.NormalTokens);
        b.WriteInt(player.SpecialTokens);
        b.WriteInt(player.Tickets);

        // 0x932c
        b.Write0(0x93b4 - 0x932c);

        b.Write0(64); // npc locations - do not matter cause they are disabled in the tables
        b.Write0(64); // pet cards
        b.Write0(64); // resources

        b.Write0(0x96d0 - 0x93f4 - 0x80);
        // 0x96d0

        Debug.Assert(b.CompressSize == 38608, "invalid PlayerData size");
        b.EndCompress();

        b.Send(client);
    }

    // 02_02
    static void SendAddPlayers(Span<Client> players, Span<Client> dest) {
        var b = new PacketBuilder(0x2, 0x2);

        b.WriteShort((short)players.Length); // count
        b.BeginCompress();
        foreach(var client in players) {
            b.WriteShort(client.Id);
            b.WriteByte(0); // status icon
            b.WriteByte(0); // guild icon
            b.WriteByte(0);

            b.Write0(16 * 2); // guild name
            b.WriteInt(0);
            b.WritePadWString(client.Player.Name, 64);
            b.Write0(65);

            b.WriteInt(0);
            b.WriteInt(client.Player.PositionX);
            b.WriteInt(client.Player.PositionY);

            b.WriteByte(client.Player.Rotation);
            b.WriteByte(0);
            b.WriteByte(client.Player.Speed); // speed
            b.WriteByte(client.Player.Gender);

            client.Player.WriteEntities(b);

            b.WriteInt(0);
            b.WriteInt(0);

            b.WriteByte(0); // player title
        }
        b.EndCompress();

        b.Send(dest);
    }

    // 02_03
    public static void SendDeletePlayer(IEnumerable<Client> clients, Client leaving) {
        var b = new PacketBuilder(0x2, 0x3);

        b.WriteShort(leaving.Id);
        b.WriteShort(0); // unused?

        b.Send(clients);
    }

    // 02_04
    static void BroadcastMovePlayer(Client client) {
        var b = new PacketBuilder(0x2, 0x4);

        b.WriteShort(client.Id);
        b.WriteInt(client.Player.PositionX);
        b.WriteInt(client.Player.PositionY);
        b.WriteShort(client.Player.Speed);

        b.Send(client.Player.Map.Players.Where(x => x != client));
    }

    // 02_05
    static void BroadcastPlayerStatus(Client client) {
        var b = new PacketBuilder(0x2, 0x5);

        b.WriteShort(client.Id);
        b.WriteInt(client.Player.Status);

        b.Send(client.Player.Map.Players.Where(x => x != client));
    }

    // 02_06
    static void BroadcastPlayerEmote(Client client, int emote) {
        var b = new PacketBuilder(0x2, 0x6);

        b.WriteShort(client.Id);
        b.WriteInt(emote);

        b.Send(client.Player.Map.Players);
    }

    // 02_07
    static void SendRotatePlayer(Client client) {
        var b = new PacketBuilder(0x2, 0x7);

        b.WriteShort(client.Id);
        b.WriteShort(client.Player.Rotation);

        b.Send(client.Player.Map.Players.Where(x => x != client));
    }

    // 02_08
    static void SendPlayerState(Client client) {
        var b = new PacketBuilder(0x2, 0x8);

        b.WriteShort(client.Id);
        b.WriteShort(client.Player.State);

        b.Send(client.Player.Map.Players.Where(x => x != client));
    }

    // 02_09
    public static void SendChangeMap(Client client) {
        var b = new PacketBuilder(0x02, 0x9);
        var player = client.Player;

        b.WriteInt(player.CurrentMap);
        b.WriteShort((short)player.PositionX);
        b.WriteShort((short)player.PositionY);
        b.WriteByte(0);

        if(player.MapType == 3) {
            var farm = (Server.Farm)player.Map;

            b.WriteCompressed(farm);

            b.WriteInt(farm.OwnerId);
            b.WriteString(farm.OwnerName, 1);
            b.WriteByte(0);
            b.WriteByte(0);

            b.BeginCompress();
            farm.WriteLocked(b);
            b.EndCompress();

            b.WriteInt(farm.House.BuildingPermit);
        } else if(player.MapType == 4) {
            var house = ((HouseFloor)player.Map).House;

            b.BeginCompress(); // buildingData_1
            b.WriteInt(house.BuildingPermit);
            b.Write0(16); // idk

            for(int i = 0; i < 200; i++) {
                if(house.Furniture.TryGetValue(i, out var value)) {
                    b.Write(value);
                } else {
                    b.Write0(24);
                }
            }

            b.EndCompress();

            b.BeginCompress(); // buildingData_2
            // 3 sets depending on mapId % 10

            // 0, 1, 2 = outside
            b.WriteInt(house.Floor0.ViewId);
            b.WriteInt(house.Floor1.ViewId);
            b.WriteInt(house.Floor2.ViewId);

            // 3, 4, 5 = floor
            b.WriteInt(house.Floor0.FloorId);
            b.WriteInt(house.Floor1.FloorId);
            b.WriteInt(house.Floor2.FloorId);

            // 6, 7, 8 = wall
            b.WriteInt(house.Floor0.WallId);
            b.WriteInt(house.Floor1.WallId);
            b.WriteInt(house.Floor2.WallId);

            // 9,10,11 = window
            b.WriteInt(house.Floor0.WindowId);
            b.WriteInt(house.Floor1.WindowId);
            b.WriteInt(house.Floor2.WindowId);

            // byte current house floor

            b.Write0(52); // rest of buildingData_2

            b.WriteShort(0); // idk

            // floor placeable
            for(int i = 0; i < 16 * 20; i++) {
                b.WriteByte(1);
            }

            b.EndCompress();
        }

        b.WriteByte(0);
        /* if(byte == 99) {
            // have_data
            b.Add((int)0);
            b.AddString("", 2);
        } */

        b.Send(client);
    }

    // 02_0A
    // static void DeletePlayer

    // 02_0B
    // knocked out?

    // 02_0C
    public static void SendPlayerAtt(Client client) {
        var b = new PacketBuilder(0x2, 0xC);

        b.WriteShort(client.Id);

        b.WriteShort(18 * 4); // size
        for(int i = 0; i < 18; i++) {
            b.WriteInt(client.Player.DisplayEntities[i]);
        }

        b.Send(client);
    }

    // 02_0E
    public static void SendSkillChange(Client client, Skill skill, bool showMessage) {
        var b = new PacketBuilder(0x02, 0x0E);

        b.WriteByte((byte)skill);
        b.WriteShort(client.Player.Levels[(int)skill]);
        b.WriteInt(client.Player.Exp[(int)skill]);
        b.WriteByte(Convert.ToByte(showMessage));

        b.Send(client);
    }

    // 02_0F
    static void BroadcastTeleportPlayer(Client client) {
        var b = new PacketBuilder(0x02, 0x0F);

        b.WriteShort(client.Id);
        b.WriteInt(client.Player.PositionX);
        b.WriteInt(client.Player.PositionY);

        b.Send(client.Player.Map.Players);
    }

    // 02_11
    public static void SendSetEquItem(Client client, InventoryItem item, byte position, bool tool) {
        var b = new PacketBuilder(0x02, 0x11);

        b.WriteCompressed(item);
        b.WriteByte(position); // position
        b.WriteByte((byte)(tool ? 2 : 1)); // action
        b.WriteByte(0); // play sound

        b.Send(client);
    }

    // 02_12
    public static void SendPlayerHpSta(Client client) {
        var b = new PacketBuilder(0x02, 0x12);

        b.WriteShort(client.Id); // player id

        var player = client.Player;

        b.BeginCompress();
        player.WriteStats(b);
        b.EndCompress();

        b.Send(client);
    }

    static void writeTeleport(PacketBuilder w, Teleport tp) {
        w.WriteInt(tp.Id);
        w.WriteInt(tp.FromX);
        w.WriteInt(tp.FromY);
        w.WriteInt(tp.QuestFlag);
        w.WriteByte((byte)tp.Rotation);
        w.Write0(3); // unused
        w.WriteInt(tp.TutorialFlag);
        w.WriteInt(tp.DreamRoomNum);
        w.WriteInt(tp.KeyItem); // consumeItem
        w.WriteInt(tp.KeyItemCount); // consumeItemCount
        w.WriteByte((byte)tp.SomethingRotation);
        w.WriteByte(0); // unused
        w.WriteShort((short)tp.WarningStringId);
        w.WriteInt(0); // keyItem
    }

    // 02_14 / 02_15
    static void SendTeleporters(Client client, IReadOnlyCollection<Teleport> teleporters) {
        var b = new PacketBuilder(0x02, 0x14);

        b.WriteInt(teleporters.Count); // count

        b.BeginCompress();
        foreach(var teleporter in teleporters) {
            writeTeleport(b, teleporter);
        }
        b.EndCompress();

        b.Send(client);
    }

    static void writeNpcData(PacketBuilder w, NpcData npc) {
        w.WriteInt(npc.Id);
        w.WriteInt(npc.X);
        w.WriteInt(npc.Y);

        w.WriteByte((byte)npc.Rotation);
        w.Write0(3); // unused

        w.WriteInt((int)npc.Action1);
        w.WriteInt((int)npc.Action2);
        w.WriteInt((int)npc.Action3);
        w.WriteInt((int)npc.Action4);
    }

    // 02_16
    static void SendNpcs(Client client, IReadOnlyCollection<NpcData> npcs) {
        // create npcs
        var b = new PacketBuilder(0x02, 0x16);

        b.WriteInt(npcs.Count); // count

        b.BeginCompress();
        foreach(var npc in npcs) {
            writeNpcData(b, npc);
        }
        b.EndCompress();

        b.Send(client);
    }

    static void writeResData(PacketBuilder w, Extractor.Resource res) {
        w.WriteInt(res.Id); // entity/npc id
        w.WriteInt(res.X); // x 
        w.WriteInt(res.Y); // y

        w.WriteShort(res.ResourceType); // nameId
        w.WriteShort(res.Level); // count

        w.WriteByte(1); // rotation
        w.Write0(3); // unused

        w.WriteShort(res.Type1);
        w.WriteShort(res.LootTable2 == 0 ? (short)3 : res.Type2);

        w.WriteByte(0); // 5 = no lan man?
        w.Write0(3); // unused
    }
    // 02_17
    static void SendRes(Client client, IReadOnlyCollection<Extractor.Resource> resources) {
        // create npcs
        var b = new PacketBuilder(0x02, 0x17);

        b.WriteInt(resources.Count); // count

        b.BeginCompress();
        foreach(var res in resources) {
            writeResData(b, res);
        }
        b.EndCompress();

        b.Send(client);
    }

    // 02_19
    static void SendCheckpoints(Client client, IReadOnlyCollection<Checkpoint> checkpoints) {
        var b = new PacketBuilder(0x02, 0x19);

        b.WriteInt(checkpoints.Count);

        b.BeginCompress();
        foreach(var checkpoint in checkpoints) {
            b.WriteInt(checkpoint.Id);
            b.WriteInt(checkpoint.X);
            b.WriteInt(checkpoint.Y);
        }
        b.EndCompress();

        b.Send(client);
    }

    public enum MessageType : short {
        Pet_level_cap_reached = 0x01,
        You_need_to_have_a_pet_with_you = 0x02,
        Pet_quota_is_full = 0x03,
        Pets_level_does_not_meet_item_requirement_Item_cannot_be_used = 0x04,
        Not_enough_training_points = 0x05,
        Pet_released = 0x06,
        You_cannot_remove_this_pet_while_it_is_still_following_you = 0x07,
        You_do_not_have_enough_Action_Points_1 = 0x0b,
        Quest_Log_is_full = 0x0c,
        You_cannot_use_this_item_level_requirement_not_met = 0x15,
        Player_is_not_on_this_server = 0x1f,
        Your_Friends_list_is_full = 0x29,
        You_cannot_add_yourself_to_your_own_Friends_list = 0x2a,
        Cannot_find_player = 0x2b,
        Player_is_already_in_your_Friends_list = 0x2c,
        // You_cannot_add_yourself_to_your_own_Friends_list = 0x2d,
        Blacklist_is_full = 0x2e,
        Player_is_already_in_your_blacklist = 0x2f,
        // Cannot_find_player = 0x33,
        Player_is_busy = 0x34,
        // Player_is_not_on_this_server = 0x35,
        Player_is_the_leader_already = 0x36,
        Guild_is_full_when_trying_to_add_player_to_guild = 0x37,
        Player_is_in_another_guild = 0x38,
        House_is_locked = 0x3d,
        The_house_owner_is_currently_in_Decoration_Mode_Please_wait_until_the_house_owner_has_finished_decorating = 0x3e,
        Cannot_demolish_your_house_Make_sure_there_is_no_furniture_inside_and_try_again = 0x3f,
        Player_demolished_house = 0x40,
        // House_is_locked = 0x41,
        You_do_not_have_enough_money_1 = 0x48,
        Your_Friendship_Level_with_this_faction_is_too_low = 0x49,
        You_do_not_have_enough_Dream_Fragments = 0x4a,
        You_do_not_have_enough_Tickets = 0x4b,
        Failed_to_sell_item = 0x4c,
        You_do_not_have_enough_normal_tokens = 0x4d,
        You_do_not_have_enough_special_tokens = 0x4e,
        Not_enough_Magic_Crystals = 0x51,
        Trade_complete = 0x5b,
        Trade_failed = 0x5c,
        Trade_cancelled = 0x5d,
        Player_is_busy_right_now = 0x5e,
        NON_TRANSFERABLE_TO_PLAYER = 0x5f,
        You_cannot_transfer_this_item_to_that_player = 0x60,
        You_cannot_drop_this_item = 0x61,
        You_cannot_use_this_item = 0x62,
        // You_cannot_drop_this_item = 0x63,
        Inventory_full = 0x65,
        Farm_owners_inventory_is_full = 0x66,
        You_cannot_take_items_from_the_Item_Delivery_when_youre_in_Dream_Carnival = 0x69,
        Item_Delivery_is_disabled_in_This_Map = 0x6a,
        Unable_to_enhance = 0x6f,
        New_enhance_level_is_too_low = 0x70,
        You_cannot_use_pets_until_you_reach_level_10 = 0x78,
        You_can_equip_only_one_pet_at_your_current_level_An_extra_pet_slot_will_open_when_you_reach_level_20 = 0x79,
        You_can_equip_only_two_pets_at_your_current_level__An_extra_pet_slot_will_open_when_you_reach_level_30 = 0x7a,
        Your_pet_has_abandoned_you_as_a_result_of_neglect = 0x7b,
        You_cannot_send_a_private_message_to_your_friend_at_this_time_because_he_she_has_been_muted = 0x82,
        You_have_been_kicked_out_by_the_house_owner = 0x83,
        This_item_can_only_be_used_when_you_are_collecting_materials_for_your_house_construction = 0x84,
        Sorry_System_allows_player_to_use_a_maximum_of_10_functional_type_consumable_item_simultaneously = 0x85,
        Sorry_You_already_used_that_item = 0x86,
        Enable_party_avatar_checking_for_entering_special_map = 0x87,
        Disable_party_avatar_checking_for_entering_special_map = 0x88,
        Action_failed_You_didnt_get_anything = 0x8c,
        Your_items_are_now_good_as_new = 0x8d,
        Wand_recharged = 0x8e,
        Wrong_Blood_Type = 0x91,
        Wrong_Birth_Month = 0x92,
        Wrong_Birth_Day = 0x93,
    }

    public static void SendMessage(Client client, MessageType message) {
        var b = new PacketBuilder(0x02, 0x1F);

        b.WriteShort((short)message);

        b.Send(client);
    }

    // 02_20
    static void SendOtherPlayerInfo(Client client, PlayerData other) {
        var b = new PacketBuilder(0x02, 0x20);

        b.BeginCompress();

        b.WritePadWString(other.Name, 32 * 2);
        b.WriteByte(other.Gender);
        b.Write0(11);
        b.WriteByte(other.BloodType);
        b.WriteByte(other.BirthMonth);
        b.WriteByte(other.BirthDay);
        b.WriteByte(other.GetConstellation());

        other.WriteEntities(b);

        for(int i = 0; i < 14; i++)
            b.Write(other.Equipment[i]); // equipment
        for(int i = 0; i < 6; i++)
            b.Write(new InventoryItem()); // inv2

        b.WritePadString(other.FavoriteFood, 38);
        b.WritePadString(other.FavoriteMovie, 26);
        b.WritePadWString(other.Location, 36 * 2);
        b.WritePadString(other.FavoriteMusic, 66);
        b.WritePadWString(other.FavoritePerson, 64 * 2);
        b.WritePadWString(other.Hobbies, 160 * 2);
        b.WritePadWString(other.Introduction, 160 * 2);
        b.Write0(3);

        other.WriteStats(b);

        b.EndCompress();

        b.Send(client);
    }

    // 02_61_01
    public static void Send61_01(Client client, string msg) {
        var b = new PacketBuilder(0x02, 0x61);

        b.WriteByte(0x01); // third switch

        b.WriteString(msg, 1);

        b.Send(client);
    }

    // 02_6E
    static void SendUpdatePackage(Client client, int mapId, string package) {
        var b = new PacketBuilder(0x02, 0x6E);

        b.WriteWString(""); // special message
        b.WriteInt(mapId); // map id
        b.WriteString(package, 1); // package name

        b.Send(client);
    }

    // 02_6F
    static void Send02_6F(Client client) {
        var b = new PacketBuilder(0x02, 0x6F);

        b.WriteByte(0);

        b.Send(client);
    }

    #endregion
}
