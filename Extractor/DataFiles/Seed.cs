namespace Extractor;

[SeanItem(21)]
public struct Seed {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int PlantAppearanceId { get; set; }
    [SeanField(2)] public int Level { get; set; }

    [SeanField(03)] public int Unknown03 { get; set; }
    [SeanField(04)] public int Unknown04 { get; set; }

    [SeanField(5)] public int MaxPlantAmount { get; set; } // always 10

    [SeanField(06)] public int Unknown06 { get; set; }
    [SeanField(07)] public int Unknown07 { get; set; }
    [SeanField(08)] public int Unknown08 { get; set; }
    [SeanField(09)] public int Unknown09 { get; set; }
    [SeanField(10)] public int Unknown10 { get; set; }

    [SeanField(11)] public int GatherLoot { get; set; }
    [SeanField(12)] public int ChopLoot { get; set; }

    [SeanField(13)] public int Unknown13 { get; set; }
    [SeanField(14)] public int Unknown14 { get; set; }
    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }
    [SeanField(17)] public int Unknown17 { get; set; }
    [SeanField(18)] public int Unknown18 => 0;

    [SeanField(19)] public int ExpireTime { get; set; } // in minutes

    [SeanField(20)] public int Unknown20 { get; set; }
}
