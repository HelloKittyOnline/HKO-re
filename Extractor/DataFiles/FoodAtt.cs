namespace Extractor;

[SeanItem(11)]
public struct FoodAtt {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int EnergyRestored { get; set; }
    [SeanField(2)] public int ActionPointsRestored { get; set; }
    [SeanField(3)] public int Duration { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public int Unknown5 { get; set; }
    [SeanField(6)] public int Unknown6 => 0;
    [SeanField(7)] public string FileName { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }
    [SeanField(9)] public int Unknown9 { get; set; }
    [SeanField(10)] public int Unknown10 => 0;
}
