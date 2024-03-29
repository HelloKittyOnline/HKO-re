﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Protocols;

static class Npc {
    static int GetNextDialog(Client client, int npcId) {
        var dialogs = Program.questMap[npcId];

        foreach(var item in dialogs) {
            if(item.CheckRequirements(client)) {
                return item.Dialog;
            }
        }

        return 0;
    }

    public static void UpdateQuestMarkers(Client client, IEnumerable<int> npcs) {
        var disable = new List<int>();
        var enable = new List<(int, int)>();

        foreach(var npc in npcs) {
            var dialog = GetNextDialog(client, npc);

            if(dialog == 0)
                disable.Add(npc);
            else
                enable.Add((npc, dialog));
        }

        SetQuestMarkers(client, disable, enable);
    }

    #region Request
    [Request(0x05, 0x01)] // 00573de8
    static void GetNpcDialog(ref Req req, Client client) {
        var npcId = req.ReadInt32();
        int dialog = GetNextDialog(client, npcId);

        lock(client.Player) {
            if(Program.npcEncyclopedia.TryGetValue(npcId, out var id)) {
                client.Player.Npcs.Add(id);
            }
        }

        SendOpenDialog(client, dialog);
    }

    [Request(0x05, 0x02)] // 00573e4a // npc data ack?
    static void Recv02(ref Req req, Client client) { }

    [Request(0x05, 0x03)] // 00573ef2
    static void TakeQuest(ref Req req, Client client) {
        var npcId = req.ReadInt32();
        var questId = req.ReadInt32();
        var dialogId = req.ReadInt32();
        var rewardSelect = req.ReadInt32();

        // todo: use questID instead of dialogID?
        var sub = Program.questMap[npcId].FirstOrDefault(x => x.Dialog == dialogId);
        if(sub is null)
            return; // wrong id?

        lock(client.Player) {
            if(rewardSelect == 0 && sub.Rewards.Any(x => x is Reward.Select))
                return; // no reward selected

            if(!sub.CheckRequirements(client))
                return; // requirements not met

            if(!sub.CheckRewards(client)) {
                Player.SendMessage(client, Player.MessageType.Inventory_full);
                return; // not enough inv space for reward
            }

            if(client.Player.QuestFlags.Count(x => x.Value == QuestStatus.Running) >= 10) {
                Player.SendMessage(client, Player.MessageType.Quest_Log_is_full);
                return; // quest limit reached
            }

            foreach(var requ in sub.Requirements) {
                if(requ is Requirement.GiveItem item) {
                    client.RemoveItem(item.Id, item.Count);
                }
            }

            foreach(var reward in sub.Rewards) {
                reward.Handle(client, rewardSelect);
            }

            // TODO: make this more efficient - could build a quest graph to determine which npcs have to be updated
            UpdateQuestMarkers(client, client.Player.Map.Npcs.Select(x => x.Id));
        }
    }

    [Request(0x05, 0x04)] // 00573f74 // quest requirement completed
    static void UpdateQuest(ref Req req, Client client) {
        var npcId = req.ReadInt32();
        var dialog = GetNextDialog(client, npcId);
        SetQuestMarker(client, npcId, dialog);
    }

    [Request(0x05, 0x05)] // // 00573fe8
    public static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }

    [Request(0x05, 0x06)] // 0057405f
    static void CancelQuest(ref Req req, Client client) {
        var questId = req.ReadInt32();

        // TODO: gracefully handle canceling quests
        if(client.Player.QuestFlags[questId] == QuestStatus.Running)
            client.Player.QuestFlags[questId] = QuestStatus.None;
        SendDeleteQuest(client, questId);
    }

    [Request(0x05, 0x07)] // // 005740ca
    public static void Recv07(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x05, 0x08)] // // 0057414f
    public static void Recv08(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x05, 0x09)] // // 005741fe
    public static void Recv09(ref Req req, Client client) { throw new NotImplementedException(); }

    [Request(0x05, 0x0A)] // 005742b7
    static void FinishMinigame(ref Req req, Client client) {
        var gameId = req.ReadInt32();
        var score = req.ReadInt32();

        // could be used to store quest id
        var param1 = req.ReadInt32(); // arbitrary param 1
        var param2 = req.ReadInt32(); // arbitrary param 2

        if(!Program.minigameQuests.TryGetValue(gameId, out var quest))
            return; // quest not found?

        if(quest.Minigame.Score > score)
            return; // score not met

        if(client.Player.QuestFlags.Count(x => x.Value == QuestStatus.Running) >= 10) {
            Player.SendMessage(client, Player.MessageType.Quest_Log_is_full);
            return; // quest limit reached
        }

        lock(client.Player) {
            if(!quest.Sections.Any(x => x.Rewards.Any(y => y is Reward.StartMinigame) && x.CheckRequirements(client)))
                return; // starting condition not met

            if(client.Player.QuestFlags.TryGetValue(quest.Id, out var flag) && flag == QuestStatus.Done)
                return; // quest already completed so no reward

            client.Player.QuestFlags[quest.Id] = QuestStatus.Running;
        }
        SendNewQuest(client, quest.Id);
        UpdateQuestMarkers(client, client.Player.Map.Npcs.Select(x => x.Id));
    }

    [Request(0x05, 0x0B)] // // 0057431e
    public static void Recv0B(ref Req req, Client client) { throw new NotImplementedException(); }

    [Request(0x05, 0x0C)] // 0057438c
    static void CollectCheckpoint(ref Req req, Client client) {
        var id = req.ReadInt32();

        lock(client.Player) {
            client.Player.CheckpointFlags.TryGetValue(id, out var val);
            if(val != 1)
                return;

            var dat = Program.checkpoints[id];

            // check if player has required item
            if(client.GetInv(InvType.Player).GetItemCount(dat.ConsumeItem) < dat.ConsumeItemCount)
                return;

            // give player reward item
            if(!client.AddItem(dat.Item, dat.ItemCount, true))
                return;

            // remove required item
            client.RemoveItem(dat.ConsumeItem, dat.ConsumeItemCount);

            client.Player.CheckpointFlags[id] = 2;
            UpdateFlag(client, dat.ActiveQuestFlag, false);
            if(dat.CollectedQuestFlag != 0)
                UpdateFlag(client, dat.CollectedQuestFlag, true);
            UpdateQuestMarkers(client, client.Player.Map.Npcs.Select(x => x.Id));
        }
    }

    [Request(0x05, 0x11)] // // 00574400
    public static void Recv11(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x05, 0x14)] // // 0057448b
    public static void Recv14(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x05, 0x15)] // // 00574503
    public static void Recv15(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x05, 0x16)] // // 00574580 // restart tutorial?
    public static void Recv16(ref Req req, Client client) { throw new NotImplementedException(); }
    #endregion

    #region Response
    // 05_01
    public static void SendOpenDialog(Client client, int dialogId) {
        var b = new PacketBuilder(0x05, 0x01);

        b.WriteInt(dialogId); // dialog id (0 == npc default)

        b.Send(client);
    }

    // 05_02
    public static void SetQuestMarkers(Client client, IList<int> disable, IList<(int npc, int dialog)> enable) {
        var b = new PacketBuilder(0x05, 0x02);

        // disable quest markers
        b.WriteInt(disable.Count);
        for(int i = 0; i < disable.Count; i++) {
            b.WriteInt(disable[i]); // npc id
        }

        // enable quest markers
        b.WriteInt(enable.Count);
        for(int i = 0; i < enable.Count; i++) {
            b.WriteInt(enable[i].npc); // npc id
            b.WriteInt(enable[i].dialog); // dialog id?
        }

        b.Send(client);
    }

    // 05_04
    public static void SetQuestMarker(Client client, int npcId, int dialogId) {
        var b = new PacketBuilder(0x05, 0x04);

        // set quest marker
        b.WriteInt(npcId);
        b.WriteInt(dialogId); // dialog id (0 == disable)

        b.Send(client);
    }

    // 05_05
    public static void Send05_05(Client client, int dialogId) {
        var b = new PacketBuilder(0x05, 0x05);

        // something timer related
        b.WriteInt(0); // npc id
        b.WriteInt(0); // idk
        b.WriteInt(0); // time in seconds

        b.Send(client);
    }

    // 05_06
    public static void SendDeleteQuest(Client client, int questId) {
        var b = new PacketBuilder(0x05, 0x06);

        b.WriteInt(questId);

        b.Send(client);
    }

    // 05_07
    public static void SendQuestExpired(Client client, int questId) {
        var b = new PacketBuilder(0x05, 0x07);

        // something timer related
        b.WriteInt(questId);

        b.Send(client);
    }


    // 05_09
    public static void SendOpenMinigame(Client client, int minigameId, int requiredScore, int param1, int param2) {
        var b = new PacketBuilder(0x05, 0x09);

        b.WriteInt(minigameId);
        b.WriteInt(0); // para1
        b.WriteInt(0); // para2
        b.WriteInt(requiredScore);
        b.WriteInt(0); // (10000 = play movie)

        // will be sent back
        b.WriteInt(param1);
        b.WriteInt(param2);

        b.Send(client);
    }

    // 05_0A
    public static void UpdateFlag(Client client, int flag, bool set) {
        var b = new PacketBuilder(0x05, 0x0A);

        b.WriteInt(flag);
        b.WriteByte(Convert.ToByte(set));

        b.Send(client);
    }

    // 05_0B
    public static void SetQuestFlag(Client client, int questId, byte flagId) {
        var b = new PacketBuilder(0x05, 0x0B);

        b.WriteInt(questId);
        b.WriteByte(flagId);

        b.Send(client);
    }

    // 05_0C
    public static void SendNewQuest(Client client, int questId) {
        var b = new PacketBuilder(0x05, 0x0C);

        b.WriteInt(questId);

        b.Send(client);
    }

    // 05_0F
    public static void SendSetFriendship(Client client, byte village) {
        var b = new PacketBuilder(0x05, 0x0F);

        b.WriteByte(village);
        b.WriteShort(client.Player.Friendship[village - 1]);

        b.Send(client);
    }

    // 05_14
    public static void Send05_14(Client client) {
        var b = new PacketBuilder(0x05, 0x14);

        b.WriteByte(0x01);

        b.WriteString("https://google.de", 1);

        b.Send(client);
    }
    #endregion
}
