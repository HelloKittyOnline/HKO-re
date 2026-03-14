namespace Extractor;

[SeanItem(2)]
struct PetPrice {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Price => 0;
}
