namespace Extractor;

[SeanItem(5)]
public struct Key {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public string Description { get; set; }
    [SeanField(3)] public string BigImage { get; set; }
    [SeanField(4)] public string CardImage { get; set; }
}
