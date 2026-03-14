namespace Extractor;

[SeanItem(7)]
struct Billboard {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Unknown1 { get; set; }
    [SeanField(2)] public int Unknown2 { get; set; }
    [SeanField(3)] public int Unknown3 { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public string FileName { get; set; }
    [SeanField(6)] public string Url { get; set; }
}
