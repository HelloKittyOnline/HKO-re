namespace Extractor;

[SeanItem(13)]
public struct Checkpoint {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Map { get; set; }
    [SeanField(2)] public int X { get; set; }
    [SeanField(3)] public int Y { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public int Item { get; set; }
    [SeanField(6)] public int ItemCount { get; set; }
    [SeanField(7)] public int ActiveQuestFlag { get; set; }
    [SeanField(8)] public int CollectedQuestFlag { get; set; }
    [SeanField(9)] public string Path { get; set; }
    [SeanField(10)] public int ConsumeItem { get; set; }
    [SeanField(11)] public int ConsumeItemCount { get; set; }
    [SeanField(12)] public int Unknown12 => 0;
}
