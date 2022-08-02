namespace Extractor;

[SeanItem(17)]
public struct InGameTutorialData {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Section { get; set; }
    [SeanField(2)] public int Step { get; set; }
    [SeanField(3)] public int SubStep { get; set; }

    [SeanField(4)] public string Text { get; set; }
    [SeanField(5)] public int Map { get; set; }

    [SeanField(6)] public int X1 { get; set; }
    [SeanField(7)] public int Y1 { get; set; }
    [SeanField(8)] public int X2 { get; set; }
    [SeanField(9)] public int Y2 { get; set; }

    [SeanField(10)] public string Str2 { get; set; }
    [SeanField(11)] public int SubId { get; set; }
    [SeanField(12)] public string Str3 { get; set; }

    // something to do with the tutorial window position
    [SeanField(13)] public int X3 { get; set; }
    [SeanField(14)] public int Y3 { get; set; }
    [SeanField(15)] public int X4 { get; set; }
    [SeanField(16)] public int Y5 { get; set; }
}
