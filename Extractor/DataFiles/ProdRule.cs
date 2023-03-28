using System;

namespace Extractor;

[SeanItem(25)]
public struct ProdRule {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int ItemId { get; set; }
    [SeanField(2)] public int RequiredLevel { get; set; }
    [SeanField(3)] public int Count { get; set; }
    [SeanArray(4, 5)] public Item[] Ingredients { get; set; }

    public struct Item {
        [SeanField(0)] public int ItemId { get; set; }
        [SeanField(1)] public int Count { get; set; }
    }

    public Skill GetSkill() {
        return (Id / 512) switch {
            0 => Skill.Forging,
            1 => Skill.Forging,
            2 => Skill.Carpentry,
            3 => Skill.Carpentry,
            4 => Skill.Cooking,
            5 => Skill.Cooking,
            6 => Skill.Tailoring,
            7 => Skill.Tailoring,
            8 => Skill.Tailoring,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
