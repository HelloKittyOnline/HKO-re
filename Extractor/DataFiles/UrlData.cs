namespace Extractor;

[SeanItem(4)]
public struct UrlData {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Path { get; set; }
    [SeanField(2)] public string Hash { get; set; }
    [SeanField(3)] public string Name { get; set; }
}
