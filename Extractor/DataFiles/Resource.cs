using System.Diagnostics;

namespace Extractor;

[SeanItem(17)]
public struct Resource {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int MapId { get; set; }
    [SeanField(2)] public int X { get; set; }
    [SeanField(3)] public int Y { get; set; }

    [SeanField(4)] public short NameId { get; set; }
    [SeanField(5)] public short Level { get; set; }

    [SeanField(6)] public int LootTable { get; set; }

    [SeanField(7)] public int Tool1 { get; set; }
    [SeanField(8)] public int Tool2 { get; set; }

    [SeanField(9)] public byte Type1 { get; set; }

    [SeanField(10)] public int Unknown10 { get; set; }
    [SeanField(11)] public int Unknown11 { get; set; }
    [SeanField(12)] public int Unknown12 { get; set; }

    [SeanField(13)] public byte Type2 { get; set; }

    [SeanField(14)] public int Unknown14 => 0;
    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }

    public Skill GetSkill(int action) {
        Debug.Assert(action is 1 or 2);
        var type = action switch {
            1 => Type1,
            2 => Type2,
            _ => 99
        };

        Debug.Assert(type is 0 or 1 or 2 or 3);
        return type switch {
            0 => Skill.Gathering,
            1 => Skill.Mining,
            2 => Skill.Woodcutting,
            3 => Skill.Farming, // ?
            _ => Skill.General
        };
    }
}
