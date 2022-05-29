namespace Extractor {
    [SeanItem(6)]
    struct TextItem {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public string Str { get; set; }
        // [Sean(2)] public int Idk { get; set; } always 0

        [SeanField(3)] public int R { get; set; }
        [SeanField(4)] public int G { get; set; }
        [SeanField(5)] public int B { get; set; }
    }
}