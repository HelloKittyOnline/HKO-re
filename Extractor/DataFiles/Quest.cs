using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Extractor {
    public struct Quest {
        public int Id { get; set; }
        public int Village { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string[] Requirement { get; set; }
        public object[] Rewards { get; set; }
        public int Icon { get; set; }
        public int IsPartyGroupQuest { get; set; } // always 0?
        public DateTime? Expire { get; set; }
        public string[] Reset { get; set; }
        public int Type { get; set; }

        public static Quest[] Load(SeanArchive.Item[] data) {
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
                                case "Requirement": q.Requirement = buf.ToArray(); break;
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

        static dynamic[] ParseRewards(string[] vals) {
            var rewards = new List<dynamic>();

            for(var i = 0; i < vals.Length; i++) {
                var s = vals[i];
                var match = Regex.Match(s,
                    @"(?:\[i?(\d+)\](?: [x*] (\d+))?)|(?:HKO (?:Exp|Epx)  ?(\d+(?:,\d+)*))|(?:([\w ]+) Friendship (\d+(?:,\d+)*))|(?:Money (\d+(?:,\d+)*))|(?:\[im(\d+),f(\d+))\]|({c})",
                    RegexOptions.IgnoreCase);

                dynamic obj = new System.Dynamic.ExpandoObject();

                if(!match.Success) {
                    obj.type = 0;
                    obj.str = s;
                } else {
                    var item = match.Groups[1];
                    var itemCount = match.Groups[2];

                    var exp = match.Groups[3];
                    var friendPlace = match.Groups[4];
                    var friendCount = match.Groups[5];
                    var money = match.Groups[6];

                    var idk1 = match.Groups[7];
                    var idk2 = match.Groups[8];

                    var select = match.Groups[9];

                    if(item.Success) {
                        obj.type = 1;
                        obj.item = int.Parse(item.Value);
                        obj.count = itemCount.Success ? int.Parse(itemCount.Value) : 1;
                    } else if(exp.Success) {
                        obj.type = 2;
                        obj.count = int.Parse(exp.Value.Replace(",", ""));
                    } else if(friendPlace.Success) {
                        obj.type = 3;
                        obj.map = friendPlace.Value;
                        obj.count = int.Parse(friendCount.Value.Replace(",", ""));
                    } else if(money.Success) {
                        obj.type = 4;
                        obj.count = int.Parse(money.Value.Replace(",", ""));
                    } else if(select.Success) {
                        obj.type = 5;
                        obj.sub = ParseRewards(vals[(i + 1)..]);
                        i = vals.Length - 1;
                    } else if(idk1.Success) {
                        obj.type = 6;
                        obj.item = int.Parse(idk1.Value);
                        obj.f = int.Parse(idk2.Value);
                    } else {
                        throw new Exception("unreachable");
                    }
                }

                rewards.Add(obj);
            }

            return rewards.ToArray();
        }
    }
}