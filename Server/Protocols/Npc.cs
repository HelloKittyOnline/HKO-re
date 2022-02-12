using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Server.Protocols {
    class Npc {
        public static void Handle(Client client) {
            switch(client.ReadByte()) {
                case 0x01: // 00573de8
                    GetNpcDialog(client);
                    break;
                case 0x02: // 00573e4a // npc data ack?
                    break;
                case 0x03: // 00573ef2
                    TakeQuest(client);
                    break;
                /*
                case 0x05_04: // 00573f74
                case 0x05_05: // 00573fe8
                */
                case 0x06: // 0057405f
                    CancelQuest(client);
                    break;
                /*
                case 0x05_07: // 005740ca
                case 0x05_08: // 0057414f
                case 0x05_09: // 005741fe
                case 0x05_0A: // 005742b7
                case 0x05_0B: // 0057431e
                case 0x05_0C: // 0057438c
                case 0x05_11: // 00574400
                case 0x05_14: // 0057448b
                case 0x05_15: // 00574503
                case 0x05_16: // 00574580
                */

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 05_01
        static void GetNpcDialog(Client client) {
            var npcId = client.ReadInt32();
            // var npc = npcs.First(x => x.Id == npcId);

            var dialogs = Program.dialogData[npcId];
            int dialog = 0;

            foreach(var item in dialogs) {
                client.Player.QuestFlags.TryGetValue(item.Quest, out var flag);
                if(flag == 2)
                    continue; // quest already done

                if(item.Begins) {
                    if(flag == 0 && (item.Previous == -1 || client.Player.QuestFlags.TryGetValue(item.Previous, out var temp) && temp == 2)) {
                        dialog = item.Id;
                        break;
                    }
                } else {
                    if(flag == 1) {
                        // todo check for quest condition
                        dialog = item.Id;
                        break;
                    }
                }
            }

            SendOpenDialog(client.Stream, dialog);
        }

        static void HandleReward(Client client, dynamic r, int selected) {
            switch(r.type) {
                case 1:
                    client.AddItem(r.item, r.count);
                    break;
                default:
                    Debugger.Break();
                    break;
            }
        }

        // 05_03
        static void TakeQuest(Client client) {
            var npcId = client.ReadInt32();
            var questId = client.ReadInt32();
            var dialogId = client.ReadInt32();
            var select = client.ReadInt32();

            var player = client.Player;
            var res = client.Stream;

            player.QuestFlags.TryGetValue(questId, out var flag);

            if(flag == 0) {
                player.QuestFlags[questId] = 1;
                SendNewQuest(res, questId);
                if(questId == 1002) {
                    client.AddItem(2039, 1);
                }
            } else if(flag == 1) {
                // todo check condition
                player.QuestFlags[questId] = 2;

                var quest = Program.quests.First(x => x.Id == questId);

                SendQuestStatus(res, questId, true);

                if(quest.Rewards != null) {
                    foreach(dynamic r in quest.Rewards) {
                        HandleReward(client, r, select);
                    }
                }
            }
        }

        // 05_06
        static void CancelQuest(Client client) {
            var questId = client.ReadInt32();

            client.Player.QuestFlags[questId] = 0;

            SendDeleteQuest(client.Stream, questId);
        }
        #endregion

        #region Response
        // 05_01
        public static void SendOpenDialog(Stream clientStream, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(dialogId); // dialog id (0 == npc default)

            b.Send(clientStream);
        }

        // 05_02
        public static void Send05_02(Stream clientStream, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x02); // second switch

            // disable quest markers
            int n = 0;
            b.WriteInt(n);
            for(int i = 0; i < n; i++) {
                b.WriteInt(0); // npc id
            }

            // enable quest markers
            n = 0;
            b.WriteInt(n);
            for(int i = 0; i < n; i++) {
                b.WriteInt(0); // npc id
                b.WriteInt(0); // dialog id?
            }

            b.Send(clientStream);
        }

        // 05_04
        public static void Send05_04(Stream clientStream, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x04); // second switch

            // set quest marker
            b.WriteInt(0); // npc id
            b.WriteInt(0); // dialog id (0 == disable)

            b.Send(clientStream);
        }

        // 05_05
        public static void Send05_05(Stream res, int dialogId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x05); // second switch

            // something timer related
            b.WriteInt(0); // npc id
            b.WriteInt(0); // idk
            b.WriteInt(0); // time in seconds

            b.Send(res);
        }

        // 05_06
        public static void SendDeleteQuest(Stream res, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x06); // second switch

            // something timer related
            b.WriteInt(questId);

            b.Send(res);
        }

        // 05_07
        public static void SendQuestExpired(Stream res, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x06); // second switch

            // something timer related
            b.WriteInt(questId);

            b.Send(res);
        }

        public static void SendOpenMinigame(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x09); // second switch

            b.WriteInt(1); // id
            b.WriteInt(0); // para1
            b.WriteInt(0); // para2
            b.WriteInt(0); // global 1
            b.WriteInt(0); // (10000 = play movie)
            b.WriteInt(0); // global 2
            b.WriteInt(0); // global 3

            b.Send(res);
        }

        // 05_0A
        public static void SendQuestStatus(Stream res, int quest, bool completed) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x0A); // second switch

            b.WriteInt(quest);
            b.WriteByte(Convert.ToByte(completed));

            b.Send(res);
        }

        // 05_0C
        public static void SendNewQuest(Stream res, int questId) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x0C); // second switch

            b.WriteInt(questId);

            b.Send(res);
        }

        // 05_14
        public static void Send05_14(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x14); // second switch

            b.WriteByte(0x01);

            b.AddString("https://google.de", 1);

            b.Send(res);
        }
        #endregion
    }
}