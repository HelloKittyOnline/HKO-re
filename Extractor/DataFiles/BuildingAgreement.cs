namespace Extractor;

[SeanItem(22)]
public struct BuildingAgreement {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int MinFarmLevel { get; set; }

    [SeanArray(2, 6)] public Requirement[] Requirements { get; set; }

    [SeanField(14)] public int Workload { get; set; }
    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }
    [SeanField(17)] public int Unknown17 { get; set; }
    [SeanField(18)] public int Unknown18 { get; set; }
    [SeanField(19)] public string File { get; set; }
    [SeanField(20)] public int Unknown20 => 0;
    [SeanField(21)] public int Unknown21 { get; set; }

    public struct Requirement {
        [SeanField(0)] public int ItemId { get; set; }
        [SeanField(1)] public int Count { get; set; }
    }
}
