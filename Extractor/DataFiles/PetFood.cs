namespace Extractor;

[SeanItem(5)]
public struct PetFood {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int Fullness { get; set; }
    [SeanField(2)] public int ExpChance { get; set; }
    [SeanField(3)] public int ExpAmount { get; set; }
    [SeanField(4)] public int Idk { get; set; }
}
