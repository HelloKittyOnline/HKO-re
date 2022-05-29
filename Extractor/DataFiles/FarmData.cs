namespace Extractor {
    [SeanItem(9)]
    class FarmData {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(2)] public int PlotSize { get; set; }
        [SeanField(3)] public int Level { get; set; }

        [SeanField(5)] public int Idk { get; set; }
        [SeanField(7)] public int EnterX { get; set; }
        [SeanField(8)] public int EnterY { get; set; }

        [SeanField(1)] public string Map { get; set; }
        [SeanField(4)] public string Icon { get; set; }
        [SeanField(6)] public string Name { get; set; }
    }
}