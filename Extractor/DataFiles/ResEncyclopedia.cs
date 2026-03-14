namespace Extractor;

[SeanItem(7)]
struct ResEncyclopedia {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Unknown1 { get; set; }
    [SeanField(2)] public string Name { get; set; }
    [SeanField(3)] public string FileName { get; set; }
    [SeanField(4)] public int Unknown4 => 0;
    [SeanField(5)] public string Location { get; set; }
    [SeanField(6)] public int Unknown6 => 0;
}
