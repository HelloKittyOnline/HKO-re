using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Extractor {
    struct Quest {
        public string File { get; set; }
        public int Village { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string[] Requirement { get; set; }
        public string[] Reward { get; set; }
        public int Icon { get; set; }
        public int IsPartyGroupQuest { get; set; } // always 0?
        public DateTime Expire { get; set; }
        public string[] Reset { get; set; }
        public int Type { get; set; }
        
        public static Quest[] Load(SeanArchive.Item[] data) {
            var dat = new List<Quest>();

            foreach(var quest in data) {
                if(quest.Name == "qcList.txt")
                    continue;
                var contents = new SeanDatabase(quest.Contents);

                var q = new Quest();
                q.File = quest.Name;

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
                                case "Reward": q.Reward = buf.ToArray(); break;
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
    }
}