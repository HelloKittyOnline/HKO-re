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

    [SeanField(9)] public int LootTable { get; set; }
    [SeanField(10)] public int Type { get; set; }

    [SeanField(11)] public int Unknown11 { get; set; }
    [SeanField(12)] public int Unknown12 { get; set; }
    [SeanField(13)] public int Unknown13 { get; set; }

    [SeanField(14)] public string File { get; set; }
    [SeanField(15)] public int Sound { get; set; }
}
