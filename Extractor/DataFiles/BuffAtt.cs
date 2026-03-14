namespace Extractor;

[SeanItem(12)]
public struct BuffAtt {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public string Description { get; set; }
    [SeanField(3)] public int Duration { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public int Type { get; set; }
    [SeanField(6)] public int Unknown6 { get; set; }
    [SeanField(7)] public int Unknown7 { get; set; }
    [SeanField(8)] public int Unknown8 => 0;
    [SeanField(9)] public int Unknown9 => 0;
    [SeanField(10)] public string IconFile { get; set; }
    [SeanField(11)] public int Unknown11 => 0; // string
}
