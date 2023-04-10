using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Extractor;
using Server.Protocols;

namespace Server;

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

        public override bool Check(Client client) => client.GetInv(InvType.Player).GetItemCount(Id) >= Count;
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

        public override bool Check(Client client) => client.GetInv(InvType.Player).GetItemCount(Id) >= Count;
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

    public class Flags : Requirement {
        public int QuestId;
        public int[] Ids { get; set; }

        public override bool Check(Client client) {
            client.Player.QuestFlags1.TryGetValue(QuestId, out var flags);
            return Ids.All(x => (flags & (1 << x)) != 0);
        }
    }
    public class NotFlag : Requirement {
        public int QuestId;
        public int Id { get; set; }

        public override bool Check(Client client) {
            client.Player.QuestFlags1.TryGetValue(QuestId, out var flag);
            return (flag & (1 << Id)) == 0;
        }
    }

    public abstract bool Check(Client client);
}

enum Village {
    SanrioHarbour = 1,
    Florapolis = 2,
    London = 3,
    Paris = 4,
    Beijing = 5,
    DreamCarnival = 6,
    NewYork = 7,
    Tokyo = 8
}

abstract class Reward {
    public class Exp : Reward {
        public int Amount { get; set; }

        public override void Handle(Client client, int select) {
            client.AddExp(Skill.General, Amount);
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
            client.AddItem(client.Player.Gender == 1 ? Male : Female, Count, true);
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
                Npc.UpdateFlag(client, Program.checkpoints[id].ActiveQuestFlag, true);
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

    public class StartQuest : Reward {
        public int QuestId;

        public override void Handle(Client client, int select) {
            client.Player.QuestFlags[QuestId] = QuestStatus.Running;
            Npc.SendNewQuest(client, QuestId);
        }
    }

    public class EndQuest : Reward {
        public int QuestId;

        public override void Handle(Client client, int select) {
            client.Player.QuestFlags[QuestId] = QuestStatus.Done;
            client.Player.QuestFlags1.Remove(QuestId);
            Npc.UpdateFlag(client, QuestId, true);
        }
    }
    public class StartMinigame : Reward {
        public int Id;
        public int Score;

        public override void Handle(Client client, int select) {
            Npc.SendOpenMinigame(client, Id, Score, 0, 0);
        }
    }
    public class Flag : Reward {
        public int QuestId;
        public byte Id { get; set; }

        public override void Handle(Client client, int select) {
            client.Player.QuestFlags1.TryGetValue(QuestId, out var val);
            client.Player.QuestFlags1[QuestId] = val | (1u << Id);
            Npc.SetQuestFlag(client, QuestId, Id);
        }
    }

    public abstract void Handle(Client client, int select);
}


class Minigame {
    public int Id { get; set; }
    public int Score { get; set; }
}

record ManualQuest {
    public record Sub {
        public Requirement[] Requirements { get; set; }
        public int Npc { get; set; }
        public int Dialog { get; set; }
        public Reward[] Rewards { get; set; }

        public bool CheckRequirements(Client client) {
            return Requirements.All(x => x.Check(client));
        }
        public bool CheckRewards(Client client) {
            var inv = client.GetInv(InvType.Player);

            var count = Rewards.Count(x => x is Reward.Item); // max number of required inv slots
            var free = inv.FreeSlots();

            return free >= count;
        }
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Minigame Minigame { get; set; }
    public Sub[] Sections { get; set; }

    public static ManualQuest[] Load(string path) {
        var options = new JsonSerializerOptions {
            WriteIndented = true
        };
        options.Converters.Add(new RequirementConverter());
        options.Converters.Add(new RewardConverter());

        var items = JsonSerializer.Deserialize<ManualQuest[]>(File.ReadAllText(path), options);
        foreach(var item in items) {
            foreach(var section in item.Sections) {
                foreach(var req in section.Requirements) {
                    if(req is Requirement.NotFlag n)
                        n.QuestId = item.Id;
                    if(req is Requirement.Flags f)
                        f.QuestId = item.Id;
                }
                foreach(var rew in section.Rewards) {
                    switch(rew) {
                        case Reward.StartMinigame q:
                            q.Id = item.Minigame.Id;
                            q.Score = item.Minigame.Score;
                            break;
                        case Reward.Flag f:
                            f.QuestId = item.Id;
                            break;
                        case Reward.StartQuest s:
                            s.QuestId = item.Id;
                            break;
                        case Reward.EndQuest e:
                            e.QuestId = item.Id;
                            break;
                    }
                }
            }
        }

        return items;
    }
}
