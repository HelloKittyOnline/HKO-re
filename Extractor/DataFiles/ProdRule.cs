using System;

namespace Extractor;

[SeanItem(25)]
public struct ProdRule {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int RequiredLevel { get; set; }
    [SeanField(2)] public int ItemId { get; set; }
    [SeanField(3)] public int Count { get; set; }
    [SeanArray(4, 5)] public Item[] Ingredients { get; set; }
    
    public struct Item {
        [SeanField(0)] public int ItemId { get; set; }
        [SeanField(1)] public int Count { get; set; }
    }
    
    [SeanField(14)] public int Unused14 => 0;
    [SeanField(15)] public int Unused15 => 0;
    [SeanField(16)] public int Unused16 => 0;
    [SeanField(17)] public int Unused17 => 0;
    [SeanField(18)] public int Unused18 => 0;
    [SeanField(19)] public int Unused19 => 0;
    [SeanField(20)] public int Unused20 => 0;
    [SeanField(21)] public int Unused21 => 0;
    [SeanField(22)] public int Unused22 => 0;
    [SeanField(23)] public int Unused23 => 0;
    [SeanField(24)] public int Unused24 => 0;

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
