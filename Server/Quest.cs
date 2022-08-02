using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Extractor;
using Server.Protocols;

namespace Server {
    class AbstractConverter<T> : JsonConverter<T> {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options);

            var type = obj["Type"].GetValue<string>();

            var subClass = typeof(T).Assembly.GetTypes().FirstOrDefault(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(T)) && x.Name == type);
            if(subClass == null) {
                throw new Exception("Unknown Type");
            }

            return (T)obj.Deserialize(subClass, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            var type = value.GetType();
            writer.WriteString("Type", type.Name);

            foreach(var info in type.GetProperties()) {
                writer.WritePropertyName(info.Name);
                JsonSerializer.Serialize(writer, info.GetValue(value), options);
            }

            writer.WriteEndObject();
        }
    }

    class RequirementConverter : AbstractConverter<Requirement> { }
    class RewardConverter : AbstractConverter<Reward> { }

    abstract class Requirement {
        public class GiveItem : Requirement {
            public int Id { get; set; }
            public int Count { get; set; }

            public override bool Check(Client client) => client.Player.GetItemCount(Id) >= Count;
        }
        public class ClearItem : Requirement {
            public int Id { get; set; }
            public int Count { get; set; }

            public override bool Check(Client client) {
                throw new NotImplementedException();
            }
        }
        public class HaveItem : Requirement {
            public int Id { get; set; }
            public int Count { get; set; }

            public override bool Check(Client client) => client.Player.GetItemCount(Id) >= Count;
        }

        public class Idk : Requirement {
            public string Text { get; set; }

            public override bool Check(Client client) {
                // throw new NotImplementedException();
                // Debugger.Break();
                return false;
            }
        }

        public class Checkpoint : Requirement {
            public int[] Ids { get; set; }

            public override bool Check(Client client) {
                foreach(var id in Ids) {
                    client.Player.CheckpointFlags.TryGetValue(id, out var val);
                    if(val != 2)
                        return false;
                }
                return true;
            }
        }

        [Flags]
        public enum QuestFlag {
            Begin = 1,
            Running = 2,
            Done = 4
        }
        public class Quest : Requirement {
            public int Id { get; set; }
            public QuestFlag Flags { get; set; }

            public Quest() { }
            public Quest(int id, QuestFlag flags) {
                Id = id;
                Flags = flags;
            }

            public override bool Check(Client client) {
                client.Player.QuestFlags.TryGetValue(Id, out var flag);

                return ((Flags & QuestFlag.Begin) != 0 && flag is QuestStatus.None) ||
                       ((Flags & QuestFlag.Running) != 0 && flag is QuestStatus.Running) ||
                       ((Flags & QuestFlag.Done) != 0 && flag is QuestStatus.Done);
            }
        }

        public abstract bool Check(Client client);
    }

    enum Village {
        SanrioHarbour
    }

    abstract class Reward {
        public class Exp : Reward {
            public int Amount { get; set; }

            public override void Handle(Client client, int select) {
                client.Player.AddExp(client, Skill.General, Amount);
            }
        }
        public class Money : Reward {
            public int Amount { get; set; }

            public override void Handle(Client client, int select) {
                client.Player.Money += Amount;
                Inventory.SendSetMoney(client);
            }
        }
        public class Item : Reward {
            public int Female { get; set; }
            public int Male { get; set; }
            public int Count { get; set; }

            public override void Handle(Client client, int select) {
                client.AddItem(client.Player.Gender == 1 ? Male : Female, Count);
            }
        }
        public class Friendship : Reward {
            public Village Village { get; set; }
            public int Amount { get; set; }

            public override void Handle(Client client, int select) {
                client.Player.Friendship[(int)Village - 1] += (short)Amount;
                Npc.SendSetFriendship(client, (byte)Village);
            }
        }
        public class Select : Reward {
            public Item[] Sub { get; set; }

            public override void Handle(Client client, int select) {
                foreach(var item in Sub) {
                    if((client.Player.Gender == 1 ? item.Male : item.Female) == select) {
                        item.Handle(client, select);
                        break;
                    }
                }
            }
        }

        public class Checkpoint : Reward {
            public int[] Ids { get; set; }

            public override void Handle(Client client, int select) {
                foreach(var id in Ids) {
                    client.Player.CheckpointFlags.TryGetValue(id, out var val);
                    if(val != 0)
                        continue;

                    client.Player.CheckpointFlags[id] = 1;
                    Npc.UpdateFlag(client, Program.checkpoints[id].QuestFlag, true);
                }
            }
        }

        public class Key : Reward {
            public int Id { get; set; }

            public override void Handle(Client client, int select) {
                client.Player.Keys.Add(Id);
                Npc.UpdateFlag(client, Id, true);
            }
        }

        public class Dream : Reward {
            public int Id { get; set; }

            public override void Handle(Client client, int select) {
                client.Player.Dreams.Add(Id);
                Npc.UpdateFlag(client, Id, true);
            }
        }

        public abstract void Handle(Client client, int select);
    }

    class DialogData {
        public int Id { get; set; }
        public int Quest { get; set; }
        public bool Begins { get; set; }
        public int Previous { get; set; }
        public int Npc { get; set; }

        public static DialogData[] Load() {
            var dialogs = JsonSerializer.Deserialize<JsonElement[]>(File.ReadAllText("./dialog_data.json"));

            var d = new List<DialogData>();

            foreach(var dialog in dialogs) {
                if(!dialog.TryGetProperty("Previous quests", out var prev) || prev.ValueKind != JsonValueKind.Number) {
                    continue;
                }

                int quest;
                bool begins;
                if(dialog.TryGetProperty("Start", out var start) && start.ValueKind == JsonValueKind.Number) {
                    quest = start.GetInt32();
                    begins = true;
                } else if(dialog.TryGetProperty("End", out var end) && end.ValueKind == JsonValueKind.Number) {
                    quest = end.GetInt32();
                    begins = false;
                } else {
                    continue;
                }

                foreach(var a in dialog.GetProperty("Npc").EnumerateArray()) {
                    d.Add(new DialogData {
                        Id = dialog.GetProperty("Dialog").GetInt32(),
                        Quest = quest,
                        Begins = begins,
                        Previous = prev.GetInt32(),
                        Npc = a.GetInt32()
                    });
                }
            }

            return d.ToArray();
        }
    }

    class Minigame {
        public int Id { get; set; }
        public int Score { get; set; }

        public void Open(Client client) {
            Npc.SendOpenMinigame(client, Id, Score, 0, 0);
        }
    }

    class ManualQuest {
        public class Sub {
            public Requirement[] Requirements { get; set; }
            public int Npc { get; set; }
            public int Dialog { get; set; }
            public Reward[] Rewards { get; set; }

            [JsonIgnore] public ManualQuest Quest { get; set; }
            [JsonIgnore] public bool Begins { get; set; }

            public bool Check(Client client) {
                return Requirements.All(x => x.Check(client));
            }
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Sub[] Start { get; set; }
        public Sub[] End { get; set; }
        public Minigame Minigame { get; set; }

        /*
        public int Village { get; set; }
        public int Icon { get; set; }
        public DateTime? Expire { get; set; }
        public string[] Reset { get; set; }
        public int Type { get; set; }
        */

        public static ManualQuest[] Load(string path) {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.Converters.Add(new RequirementConverter());
            options.Converters.Add(new RewardConverter());

            var items = JsonSerializer.Deserialize<ManualQuest[]>(File.ReadAllText(path), options);

            foreach(var item in items) {
                foreach(var sub in item.Start) {
                    sub.Quest = item;
                    sub.Begins = true;
                }
                foreach(var sub in item.End) {
                    sub.Quest = item;
                }
            }

            return items;
        }

        public static ManualQuest[] FromNormal() {
            var quests = Quest.Load("./c_quest_eng.sdb");

            var dialogs = DialogData.Load();
            var lookup = dialogs.ToLookup(x => x.Quest);

            var o = new List<ManualQuest>();

            static Reward MapReward(QuestReward val) {
                return val switch {
                    ExpReward exp => new Reward.Exp { Amount = exp.Exp },
                    FriendshipReward friend => new Reward.Friendship { Village = (Village)friend.Village, Amount = friend.Friendship },
                    ItemReward item => new Reward.Item { Male = item.MaleItem, Female = item.FemaleItem, Count = item.Count },
                    MoneyReward money => new Reward.Money { Amount = money.Money },
                    SelectReward select => new Reward.Select { Sub = select.Sub.Select(x => MapReward(x) as Reward.Item).ToArray() },
                    _ => throw new ArgumentOutOfRangeException(nameof(val))
                };
            }

            foreach(var a in quests) {
                if(a.Title == null)
                    continue;

                var d = lookup[a.Id];

                var start = new List<Sub>();
                var end = new List<Sub>();

                var endReq = a.Requirement?.Select<QuestRequirement, Requirement>(x => {
                    switch(x) {
                        case ClearItemRequirement ci:
                            return new Requirement.ClearItem { Id = ci.Id, Count = ci.Count };
                        case FlagRequirement flagRequirement:
                            break;
                        case HaveItemRequirement hi:
                            return new Requirement.HaveItem { Id = hi.Id, Count = hi.Count };
                        case ItemRequirement item:
                            return new Requirement.GiveItem { Id = item.Id, Count = item.Count };
                        case QFlagRequirement qFlagRequirement:
                            break;
                        case RelNpc rNpc:
                            break;
                        case UpdateNpc updateNpc:
                            break;
                            // default: throw new ArgumentOutOfRangeException(nameof(x));
                    }

                    return new Requirement.Idk { Text = x.Raw };
                }).Append(new Requirement.Quest(a.Id, Requirement.QuestFlag.Running)).ToArray() ?? Array.Empty<Requirement>();
                var endReward = a.Rewards?.Select(MapReward).ToArray() ?? Array.Empty<Reward>();

                foreach(var dialog in d) {
                    if(dialog.Begins) {
                        var req = new List<Requirement> {
                            new Requirement.Quest(a.Id, Requirement.QuestFlag.Begin)
                        };
                        if(dialog.Previous != -1)
                            req.Add(new Requirement.Quest(dialog.Previous, Requirement.QuestFlag.Done));

                        start.Add(new Sub {
                            Npc = dialog.Npc,
                            Dialog = dialog.Id,
                            Rewards = Array.Empty<Reward>(),
                            Requirements = req.ToArray()
                        });
                    } else {
                        end.Add(new Sub {
                            Npc = dialog.Npc,
                            Dialog = dialog.Id,
                            Requirements = endReq,
                            Rewards = endReward
                        });
                    }
                }

                o.Add(new ManualQuest {
                    Id = a.Id,
                    Name = a.Title,
                    Description = a.Content,
                    Start = start.ToArray(),
                    End = end.ToArray()
                });
            }


            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.Converters.Add(new RequirementConverter());
            options.Converters.Add(new RewardConverter());

            var res = JsonSerializer.Serialize(o, options);
            File.WriteAllText("./quests_export.json", res);

            return o.ToArray();
        }
    }

}
