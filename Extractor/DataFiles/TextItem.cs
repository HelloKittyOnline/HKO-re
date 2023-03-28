namespace Extractor;

[SeanItem(6)]
public readonly struct TextItem {
    [SeanField(0)] public int Id { get; init; }
    [SeanField(1)] public string Str { get; init; }
    [SeanField(2)] public int Idk => 0;

    [SeanField(3)] public int R { get; init; }
    [SeanField(4)] public int G { get; init; }
    [SeanField(5)] public int B { get; init; }
}
