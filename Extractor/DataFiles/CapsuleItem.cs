namespace Extractor;

[SeanItem(6)]
struct CapsuleItem {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int MachineId { get; set; }
    [SeanField(2)] public int Group { get; set; }
    [SeanField(3)] public int ItemId { get; set; }
    [SeanField(4)] public int Count { get; set; } // 1 or 0
    [SeanField(5)] public int Tokens { get; set; } // not sure what this is for
}
