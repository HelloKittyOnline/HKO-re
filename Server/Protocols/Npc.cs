using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Server.Protocols {
    static class Npc {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01: // 00573de8
                    GetNpcDialog(client);
                    break;
                case 0x02: // 00573e4a // npc data ack?
                    break;
                case 0x03: // 00573ef2
                    TakeQuest(client);
                    break;
                case 0x04: // 00573f74 // quest requirement completed
                    UpdateQuest(client);
                    break;
                // case 0x05_05: // 00573fe8
                case 0x06: // 0057405f
                    CancelQuest(client);
                    break;
                //case 0x07: // 005740ca
                //case 0x08: // 0057414f
                //case 0x09: // 005741fe
                case 0x0A: // 005742b7
                    FinishMinigame(client);
                    break;
                //case 0x0B: // 0057431e
                case 0x0C: // 0057438c
                    CollectCheckpoint(client);
                    break;
                //case 0x11: // 00574400
                //case 0x14: // 0057448b
                //case 0x15: // 00574503
                //case 0x16: // 00574580 // restart tutorial?
                default:
                    client.LogUnknown(0x05, id);
                    break;
            }
        }

        static int GetNextDialog(Client client, int npcId) {
            var dialogs = Program.questMap[npcId];

            foreach(var item in dialogs) {
                client.Player.QuestFlags.TryGetValue(item.Quest.Id, out var flag);

                if(item.Check(client)) {
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

        public static void UpdateQuestMarker(Client client, int npc) {
            var dialog = GetNextDialog(client, npc);
            SetQuestMarker(client, npc, dialog);
        }

        #region Request
        // 05_01
        static void GetNpcDialog(Client client) {
            var npcId = client.ReadInt32();
            int dialog = GetNextDialog(client, npcId);
            SendOpenDialog(client, dialog);
        }

        // 05_03
        static void TakeQuest(Client client) {
            var npcId = client.ReadInt32();
            var questId = client.ReadInt32();
            var dialogId = client.ReadInt32();
            var rewardSelect = client.ReadInt32();

            var sub = Program.questMap[npcId].FirstOrDefault(x => x.Dialog == dialogId);

            if(sub == null)
                return; // wrong id?

            if(!sub.Check(client))
                return; // requirements not met

            if(sub.Rewards.Any(x => x is Reward.Select) && rewardSelect == 0)
                return; // no reward selected

            if(client.Player.QuestFlags.Count(x => x.Value == QuestStatus.Running) >= 10)
                return; // quest limit reached

            if(sub.Begins) {
                if(sub.Quest.Minigame != null) {
                    sub.Quest.Minigame.Open(client); // do not actually accept minigame quest until after it was completed
                    return;
                }

                client.Player.QuestFlags[questId] = QuestStatus.Running;
                SendNewQuest(client, questId);
            } else {
                client.Player.QuestFlags[questId] = QuestStatus.Done;

                foreach(var req in sub.Requirements) {
                    if(req is Requirement.GiveItem item) {
                        client.RemoveItem(item.Id, item.Count);
                    }
                }

                UpdateFlag(client, questId, true);
            }

            foreach(var reward in sub.Rewards) {
                reward.Handle(client, rewardSelect);
            }

            // TODO: make this more efficient
            UpdateQuestMarkers(client, client.Player.Map.Npcs.Select(x => x.Id));
        }

        // 05_04
        static void UpdateQuest(Client client) {
            var npcId = client.ReadInt32();
            UpdateQuestMarker(client, npcId);
        }

        // 05_06
        static void CancelQuest(Client client) {
            var questId = client.ReadInt32();

            // TODO: gracefully handle canceling quests

            // client.Player.QuestFlags[questId] = 0;
            // SendDeleteQuest(client, questId);
        }

        // 05_0A
        static void FinishMinigame(Client client) {
            var gameId = client.ReadInt32();
            var score = client.ReadInt32();

            // could be used to store quest id
            var param1 = client.ReadInt32(); // arbitrary param 1
            var param2 = client.ReadInt32(); // arbitrary param 2

            if(!Program.minigameQuests.TryGetValue(gameId, out var quest))
                return; // quest not found?

            if(!quest.Start.Any(x => x.Check(client)))
                return; // starting condition not met

            if(quest.Minigame.Score > score)
                return; // score not met

            if(client.Player.QuestFlags.TryGetValue(quest.Id, out var flag) && flag == QuestStatus.Done)
                return; // quest already completed so no reward

            client.Player.QuestFlags[quest.Id] = QuestStatus.Running;
            SendNewQuest(client, quest.Id);
            UpdateQuestMarkers(client, quest.End.Select(x => x.Npc));
        }

        // 05_0C
        static void CollectCheckpoint(Client client) {
            var id = client.ReadInt32();

            client.Player.CheckpointFlags.TryGetValue(id, out var val);
            if(val != 1)
                return;

            var dat = Program.checkpoints[id];

            if(dat.ConsumeItem != 0) {
                if(client.Player.GetItemCount(dat.ConsumeItem) < dat.ConsumeItemCount)
                    return;

                client.RemoveItem(dat.ConsumeItem, dat.ConsumeItemCount);
            }

            if(dat.Item != 0 && !client.AddItem(dat.Item, dat.ItemCount))
                return;

            client.Player.CheckpointFlags[id] = 2;
            UpdateFlag(client, dat.QuestFlag, false);
            UpdateQuestMarkers(client, client.Player.Map.Npcs.Select(x => x.Id));
        }
        #endregion

        #region Response
        // 05_01
        public static void SendOpenDialog(Client client, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(dialogId); // dialog id (0 == npc default)

            b.Send(client);
        }

        // 05_02
        public static void SetQuestMarkers(Client client, IList<int> disable, IList<(int npc, int dialog)> enable) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x02); // second switch

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
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x04); // second switch

            // set quest marker
            b.WriteInt(npcId);
            b.WriteInt(dialogId); // dialog id (0 == disable)

            b.Send(client);
        }

        // 05_05
        public static void Send05_05(Client client, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x05); // second switch

            // something timer related
            b.WriteInt(0); // npc id
            b.WriteInt(0); // idk
            b.WriteInt(0); // time in seconds

            b.Send(client);
        }

        // 05_06
        public static void SendDeleteQuest(Client client, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x06); // second switch

            b.WriteInt(questId);

            b.Send(client);
        }

        // 05_07
        public static void SendQuestExpired(Client client, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x07); // second switch

            // something timer related
            b.WriteInt(questId);

            b.Send(client);
        }


        // 05_09
        public static void SendOpenMinigame(Client client, int minigameId, int requiredScore, int param1, int param2) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x09); // second switch

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
        public static void UpdateFlag(Client client, int flag, bool completed) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x0A); // second switch

            b.WriteInt(flag);
            b.WriteByte(Convert.ToByte(completed));

            b.Send(client);
        }

        // 05_0C
        public static void SendNewQuest(Client client, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x0C); // second switch

            b.WriteInt(questId);

            b.Send(client);
        }

        // 05_0F
        public static void SendSetFriendship(Client client, byte village) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x0F); // second switch

            b.WriteByte(village);
            b.WriteShort(client.Player.Friendship[village - 1]);

            b.Send(client);
        }

        // 05_14
        public static void Send05_14(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x14); // second switch

            b.WriteByte(0x01);

            b.WriteString("https://google.de", 1);

            b.Send(client);
        }
        #endregion
    }
}