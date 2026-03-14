namespace Extractor;

[SeanItem(2)]
struct PetMsg {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Message { get; set; }
}
