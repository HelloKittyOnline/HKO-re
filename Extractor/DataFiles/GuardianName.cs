namespace Extractor;

[SeanItem(3)]
struct GuardianName {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public int Unknown2 => 0;
}
