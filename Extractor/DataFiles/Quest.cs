using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Extractor {
    public class QuestReward { 
        public string Raw { get; set; }
    }

    public class ItemReward : QuestReward {
        public int MaleItem { get; set; }
        public int FemaleItem { get; set; }
        public int Count { get; set; }

        public ItemReward(int item, int count = 1) {
            MaleItem = item;
            FemaleItem = item;
            Count = count;
        }

        public ItemReward(int maleItem, int femaleItem, int count) {
            MaleItem = maleItem;
            FemaleItem = femaleItem;
            Count = count;
        }
    }

    public class ExpReward : QuestReward {
        public int Exp { get; set; }

        public ExpReward(int exp) {
            Exp = exp;
        }
    }

    public class FriendshipReward : QuestReward {
        public int Village { get; set; }
        public int Friendship { get; set; }

        public FriendshipReward(int village, int friendship) {
            Village = village;
            Friendship = friendship;
        }
    }

    public class MoneyReward : QuestReward {
        public int Money { get; set; }

        public MoneyReward(int money) {
            Money = money;
        }
    }

    public class SelectReward : QuestReward {
        public QuestReward[] Sub { get; set; }

        public SelectReward(QuestReward[] sub) {
            Sub = sub;
        }
    }

    public class QuestRequirement {
        public string Raw { get; set; }
    }

    public class RelNpc : QuestRequirement {
        public int Id { get; set; }

        public RelNpc(int id) {
            Id = id;
        }
    }

    public class ItemRequirement : QuestRequirement {
        public int Id { get; set; }
        public int Count { get; set; }

        public ItemRequirement(int id, int count) {
            Id = id;
            Count = count;
        }
    }

    public class FlagRequirement : QuestRequirement {
        public int Id { get; set; }
        public string String { get; set; }

        public FlagRequirement(int id, string str) {
            Id = id;
            String = str;
        }
    }

    public class QFlagRequirement : QuestRequirement {
        public int Id { get; set; }
        public string String { get; set; }


        public QFlagRequirement(int id, string str) {
            Id = id;
            String = str;
        }
    }

    public class ClearItemRequirement : QuestRequirement {
        public int Id { get; set; }
        public int Count { get; set; }

        public ClearItemRequirement(int id, int count) {
            Id = id;
            Count = count;
        }
    }

    public class HaveItemRequirement : QuestRequirement {
        public int Id { get; set; }
        public int Count { get; set; }

        public HaveItemRequirement(int id, int count) {
            Id = id;
            Count = count;
        }
    }

    public class UpdateNpc : QuestRequirement {
        public int Id { get; set; }

        public UpdateNpc(int id) {
            Id = id;
        }
    }

    public class Quest {
        public int Id { get; set; }
        public int Village { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public QuestRequirement[] Requirement { get; set; }
        public QuestReward[] Rewards { get; set; }
        public int Icon { get; set; }
        public int IsPartyGroupQuest { get; set; } // always 0?
        public DateTime? Expire { get; set; }
        public string[] Reset { get; set; }
        public int Type { get; set; }

        public static Quest[] Load(string path) {
            var data = SeanArchive.Extract(path);
            var dat = new List<Quest>();

            foreach(var quest in data) {
                if(quest.Name == "qcList.txt")
                    continue;
                var contents = new SeanDatabase(quest.Contents);

                var q = new Quest();
                q.Id = int.Parse(quest.Name[..4]);

                var buf = new List<string>();
                for(int i = 1; i < contents.ItemCount; i++) {
                    var str = contents.GetString(i, 0);

                    if(str == null) continue;
                    var match = Regex.Match(str, "\\[\\[(-)?(\\w+)\\]\\]");

                    if(match.Success) {
                        if(match.Groups[1].Success) {
                            if(buf.Count == 0) continue;

                            switch(match.Groups[2].Value) {
                                case "Village": q.Village = int.Parse(buf[0]); break;
                                case "Title": q.Title = buf[0]; break;
                                case "Content": q.Content = string.Join("\n", buf); break;
                                case "Requirement": q.Requirement = ParseRequirements(buf.ToArray()); break;
                                case "Reward": q.Rewards = ParseRewards(buf.ToArray()); break;
                                case "Icon": q.Icon = int.Parse(buf[0]); break;
                                case "IsPartyGroupQuest": q.IsPartyGroupQuest = int.Parse(buf[0]); break;
                                case "Expire":
                                    q.Expire = new DateTime(
                                        int.Parse(buf[0]),
                                        int.Parse(buf[1]),
                                        int.Parse(buf[2]),
                                        int.Parse(buf[3]),
                                        int.Parse(buf[4]), 0);
                                    break;
                                case "Reset": q.Reset = buf.ToArray(); break;
                                case "Type": q.Type = int.Parse(buf[0]); break;
                                default:
                                    Debugger.Break();
                                    break;
                            }
                        } else {
                            buf.Clear();
                        }
                    } else {
                        buf.Add(str);
                    }
                }

                dat.Add(q);
            }

            return dat.ToArray();
        }

        static QuestRequirement[] ParseRequirements(string[] vals) {
            var requirements = new List<QuestRequirement>();

            var str = string.Join('\n', vals);
            var lineStart = 0;
            var pos = 0;

            string getItem() {
                if(pos == str.Length)
                    return "";

                // trim whitespace
                while(str[pos] == ' ') {
                    pos++;
                }

                var start = pos;
                if(str[pos] == '"') {
                    var end = str.IndexOf('"', pos + 1);
                    pos = end + 1;
                    return str.Substring(start + 1, end - start - 1);
                }

                while(pos < str.Length && str[pos] != ' ' && str[pos] != '"' && str[pos] != '\n') {
                    pos++;
                }

                return str[start..pos];
            }

            void nextLine() {
                while(pos < str.Length && str[pos++] != '\n') { }
                lineStart = pos;
            }

            while(pos < str.Length) {
                QuestRequirement obj = null;

                if(str[pos] == '"') {
                    getItem();
                    obj = new QuestRequirement();
                } else {
                    var s = getItem();
                    switch(s) {
                        case "rel_npc":
                            obj = new RelNpc(int.Parse(getItem()));
                            break;
                        case "item": // player has to give item
                            obj = new ItemRequirement(int.Parse(getItem()), int.Parse(getItem()));
                            break;
                        case "flag":
                            obj = new FlagRequirement(int.Parse(getItem()), getItem());
                            break;
                        case "qflag":
                            obj = new QFlagRequirement(int.Parse(getItem()), getItem());
                            break;
                        case "clear_item": // player has to get rid of all items
                            obj = new ClearItemRequirement(int.Parse(getItem()), int.Parse(getItem()));
                            break;
                        // case "pop_dlg": break;
                        case "have_item":
                            obj = new HaveItemRequirement(int.Parse(getItem()), int.Parse(getItem()));
                            break;
                        case "update_npc":
                            obj = new UpdateNpc(int.Parse(getItem()));
                            break;
                        default:
                            obj = new QuestRequirement();
                            // Console.WriteLine(s);
                            break;
                    }
                }

                obj.Raw = str[lineStart..pos];
                requirements.Add(obj);

                nextLine();
            }

            return requirements.ToArray();
        }

        static QuestReward[] ParseRewards(string[] vals) {
            var rewards = new List<QuestReward>();

            for(var i = 0; i < vals.Length; i++) {
                var s = vals[i];
                var match = Regex.Match(s, @"(?:\[i?(\d+)\](?: [x*] (\d+))?)|(?:HKO (?:Exp|Epx)  ?(\d+(?:,\d+)*))|(?:([\w ]+) Friendship (\d+(?:,\d+)*))|(?:Money (\d+(?:,\d+)*))|(?:\[im(\d+),f(\d+))\]|({c})", RegexOptions.IgnoreCase);

                QuestReward obj = null;

                if(!match.Success) {
                    switch(s) {
                        // case "Earn party points 100": break;
                        // case "Lauan 5": break;
                        case "Bamboo 5": obj = new ItemReward(1118, 5); break;
                        case "Clay 5": obj = new ItemReward(1005, 5); break;
                        case "Coal 5": obj = new ItemReward(1006, 5); break;
                        case "Shell 5": obj = new ItemReward(1001, 5); break;
                        case "Pearl 5": obj = new ItemReward(1002, 5); break;
                        case "Sugar 5": obj = new ItemReward(1709, 5); break;
                        // case "Walnut 5": break;
                        // case "4th day Key": break;
                        // case "The Spirit of Christmas * 1": break;
                        // case "Fixed Santa Pants * 1": break;
                        case "Red Scarf * 1": obj = new ItemReward(9953, 1); break;
                        case "Fixed Bell * 1": obj = new ItemReward(9941, 1); break;
                        case "Reindeer Hat * 1": obj = new ItemReward(9954, 1); break;
                        case "Santa's Reins * 1": obj = new ItemReward(9946, 1); break;
                        case "Brand New Gear * 1": obj = new ItemReward(9942, 1); break;
                        case "Santa Beard * 1": obj = new ItemReward(9955, 1); break;
                        case "Christmas Tree Hat * 1": obj = new ItemReward(9960, 1); break;
                        case "Halo Hat * 1": obj = new ItemReward(9967, 1); break;
                        case "Checkered Scarf * 1": obj = new ItemReward(9969, 1); break;
                        case "Red Snowflake Hat * 1": obj = new ItemReward(9971, 1); break;
                        case "Bowtie with Bear * 1": obj = new ItemReward(9973, 1); break;

                        // case "Badtzmaru:": break;
                        // case "Snowman Costume * 1": break; // 9974
                        // case "Snowman Hat * 1": break; // 9975
                        // case "Pochi:": break;
                        // case "Gingerbread Costume * 1": break; // 9976
                        // case "Gingerbread Hat * 1": break; // 9977

                        // case "T-Rex Card * 1": break;
                        // case "Liberty key * 1": break;
                        // case "Nothing!": continue;
                        // case "Mystery Reward": break;
                        default:
                            obj = new QuestReward();
                            continue;
                            // throw new InvalidDataException($"Missing reward data \"{s}\"");
                    }
                } else {
                    var item = match.Groups[1];
                    var itemCount = match.Groups[2];

                    var exp = match.Groups[3];
                    var friendPlace = match.Groups[4];
                    var friendCount = match.Groups[5];
                    var money = match.Groups[6];

                    var maleItem = match.Groups[7];
                    var femaleItem = match.Groups[8];

                    var select = match.Groups[9];

                    if(item.Success) {
                        obj = new ItemReward(int.Parse(item.Value), itemCount.Success ? int.Parse(itemCount.Value) : 1);
                    } else if(exp.Success) {
                        obj = new ExpReward(int.Parse(exp.Value.Replace(",", "")));
                    } else if(friendPlace.Success) {
                        obj = new FriendshipReward(friendPlace.Value switch {
                            "Sanrio Harbour" => 1,
                            "Sanrio Habour" => 1,
                            "Florapolis" => 2,
                            "London" => 3,
                            "Paris" => 4,
                            "Beijing" => 5,
                            "Dream Carnival" => 6,
                            "New York" => 7,
                            "Tokyo" => 8,
                            "Sanrio" => 2, // ?
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        int.Parse(friendCount.Value.Replace(",", "")));
                    } else if(money.Success) {
                        obj = new MoneyReward(int.Parse(money.Value.Replace(",", "")));
                    } else if(select.Success) {
                        obj = new SelectReward(ParseRewards(vals[(i + 1)..]));
                        i = vals.Length - 1;
                    } else if(maleItem.Success) {
                        obj = new ItemReward(int.Parse(maleItem.Value), int.Parse(femaleItem.Value), 1);
                    } else {
                        throw new Exception("unreachable");
                    }
                }

                obj.Raw = s;
                rewards.Add(obj);
            }

            return rewards.ToArray();
        }
    }
}