namespace Extractor;

[SeanItem(101)]
public struct PetLevelup {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1, 100)] public int[] Values { get; set; }
}
