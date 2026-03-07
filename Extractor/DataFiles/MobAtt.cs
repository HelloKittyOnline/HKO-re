namespace Extractor;

[SeanItem(26)]
public struct MobAtt {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public int Level { get; set; }
    [SeanField(3)] public int Hp { get; set; }
    [SeanField(4)] public int Attack { get; set; }
    [SeanField(5)] public int Defense { get; set; }
    [SeanField(6)] public bool Aggressive { get; set; }

    [SeanField(7)] public int Unknown7 { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }

    [SeanField(9)] public int LootTable1 { get; set; }
    [SeanField(10)] public int Type { get; set; }

    [SeanField(11)] public int Quest { get; set; }
    [SeanField(12)] public int RespawnTime { get; set; } // in seconds
    [SeanField(13)] public int Unknown13 { get; set; }

    [SeanField(14)] public string File { get; set; }
    [SeanField(15)] public int Sound { get; set; }

    [SeanField(16)] public int Unknown16 { get; set; }
    [SeanField(17)] public int LootTable2 { get; set; }
    [SeanField(18)] public int Unknown18 { get; set; }
    [SeanField(19)] public int Unknown19 { get; set; }
    [SeanField(20)] public int Unknown20 { get; set; }
    [SeanField(21)] public int Range { get; set; }
    [SeanField(22)] public int Unknown22 { get; set; }
    [SeanField(23)] public int Unknown23 => 0;
    [SeanField(24)] public int LootTable3 { get; set; } // = 0 || LootTable2
    [SeanField(25)] public int RequiredWeapon { get; set; }

}
