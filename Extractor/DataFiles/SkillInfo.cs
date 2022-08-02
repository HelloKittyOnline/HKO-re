using System;

namespace Extractor;

public enum Skill {
    General,

    Farming,
    Mining,
    Woodcutting,
    Gathering,

    Forging,
    Carpentry,
    Cooking,
    Tailoring
}

[SeanItem(10)]
public struct SkillInfo {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int Overall { get; set; }
    [SeanField(2)] public int Planting { get; set; }
    [SeanField(3)] public int Mining { get; set; }
    [SeanField(4)] public int Woodcutting { get; set; }
    [SeanField(5)] public int Gathering { get; set; }
    [SeanField(6)] public int Forging { get; set; }
    [SeanField(7)] public int Carpentry { get; set; }
    [SeanField(8)] public int Cooking { get; set; }
    [SeanField(9)] public int Tailoring { get; set; }

    public int GetExp(Skill skill) {
        return skill switch {
            Skill.General => Overall,
            Skill.Farming => Planting,
            Skill.Mining => Mining,
            Skill.Woodcutting => Woodcutting,
            Skill.Gathering => Gathering,
            Skill.Forging => Forging,
            Skill.Carpentry => Carpentry,
            Skill.Cooking => Cooking,
            Skill.Tailoring => Tailoring,
            _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
        };
    }
}
