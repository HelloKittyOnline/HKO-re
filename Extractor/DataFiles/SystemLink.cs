namespace Extractor;

[SeanItem(7)]
public struct SystemLink {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int MessageId { get; set; }
    [SeanField(2)] public string Name { get; set; }

    [SeanField(3)] public int Unknown3 => 0;
    [SeanField(4)] public int R => 0;
    [SeanField(5)] public int G => 0;
    [SeanField(6)] public int B => 0;
}
