namespace Extractor {
    [SeanItem(7)]
    struct NpcEncyclopedia {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public int NpcId { get; set; }
        [SeanField(2)] public string Description { get; set; }
        [SeanField(3)] public string Image { get; set; }

        // int Idk1 4, // always 0
        // string Idk2 5 // always null
        // string Idk3 6 // always null
    }
}