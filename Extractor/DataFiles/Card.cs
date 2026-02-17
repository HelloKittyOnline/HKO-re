namespace Extractor;

[SeanItem(13)]
public struct Card {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int Type { get; set; }
    [SeanField(2)] public int SpecialId { get; set; }
    [SeanField(3)] public int petId_or_energy { get; set; }
    [SeanField(4)] public int action_points => 0;
    [SeanField(5)] public int drain => 0;
    [SeanField(6)] public int defense => 0;
    [SeanField(7)] public int crit => 0;
    [SeanField(8)] public int dodge => 0;
    [SeanField(9)] public int exp => 0;
    [SeanField(10)] public string Unknown10 { get; set; }
    [SeanField(11)] public int collectionPos => 0;
    [SeanField(12)] public string Unknown12 { get; set; }
}
