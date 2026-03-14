namespace Extractor;

[SeanItem(10)]
struct GuardianMessage {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Unknown1 { get; set; }
    [SeanField(2)] public int Unknown2 { get; set; }
    [SeanField(3)] public string Unknown3 { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public int Unknown5 { get; set; }
    [SeanField(6)] public int Unknown6 { get; set; }
    [SeanField(7)] public int Unknown7 { get; set; }
    [SeanField(8)] public int Unknown8 => 0;
    [SeanField(9)] public int Unknown9 => 0;
}
