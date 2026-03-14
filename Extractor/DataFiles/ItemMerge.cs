namespace Extractor;



[SeanItem(33)]
struct ItemMerge {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int ItemId { get; set; }
    [SeanField(2)] public int Unknown2 { get; set; }
    [SeanField(3, 10)] public Component[] Components { get; set; }

    public struct Component {
        [SeanField(0)] public int ItemId { get; set; }
        [SeanField(1)] public int Unknown1 { get; set; }
        [SeanField(2)] public int Unknown2 { get; set; }
    }
}
