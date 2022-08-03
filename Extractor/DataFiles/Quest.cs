using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Extractor;

public class Quest {
    public int Id { get; set; }
    public int Village { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Requirements { get; set; }
    public string Rewards { get; set; }
    public int Icon { get; set; }
    public int IsPartyGroupQuest { get; set; } // always 0?
    public DateTime? Expire { get; set; }
    public string Reset { get; set; }
    public int Type { get; set; }

    public static Quest[] Load(string path) {
        var data = SeanArchive.Extract(path);
        var dat = new List<Quest>();

        foreach(var quest in data) {
            if(quest.Name == "qcList.txt")
                continue;
            var contents = new SeanDatabase(quest.Contents);

            var q = new Quest {
                Id = int.Parse(quest.Name[..4])
            };

            var str = string.Join('\n', contents.Strings.Values);

            foreach(Match match in Regex.Matches(str, @"\[\[(.+)\]\]\n(.+)\n\[\[-\1\]\]", RegexOptions.Singleline)) {
                var value = match.Groups[2].Value;

                switch(match.Groups[1].Value) {
                    case "Village": q.Village = int.Parse(value); break;
                    case "Title": q.Title = value; break;
                    case "Content": q.Content = value; break;
                    case "Requirement": q.Requirements = value; break;
                    case "Reward": q.Rewards = value; break;
                    case "Icon": q.Icon = int.Parse(value); break;
                    case "IsPartyGroupQuest": q.IsPartyGroupQuest = int.Parse(value); break;
                    case "Expire": {
                        var split = value.Split('\n');
                        q.Expire = new DateTime(
                            int.Parse(split[0]),
                            int.Parse(split[1]),
                            int.Parse(split[2]),
                            int.Parse(split[3]),
                            int.Parse(split[4]), 0);
                        break;
                    }
                    case "Reset": q.Reset = value; break;
                    case "Type": q.Type = int.Parse(value); break;
                    default:
                        Debugger.Break();
                        break;
                }
            }

            dat.Add(q);
        }

        return dat.ToArray();
    }
}
