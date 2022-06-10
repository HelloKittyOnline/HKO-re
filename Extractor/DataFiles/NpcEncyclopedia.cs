namespace Extractor {
    [SeanItem(7)]
    struct NpcEncyclopedia {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public int NpcId { get; set; }
        [SeanField(2)] public string Description { get; set; }
        [SeanField(3)] public string Image { get; set; }

        [SeanField(4)] public int Idk4 => 0;
        [SeanField(5)] public int Idk5 => 0;
        [SeanField(6)] public int Idk6 => 0;
    }
}