namespace Extractor {
    [SeanItem(25)]
    public struct MapList {
        [SeanField(0)] public int Id { get; set; }

        [SeanField(1)] public int FarmX { get; set; }
        [SeanField(2)] public int FarmY { get; set; }
        [SeanField(3)] public int SpawnX { get; set; }
        [SeanField(4)] public int SpawnY { get; set; }

        [SeanField(6)] public string Name { get; set; }
        [SeanField(7)] public string File { get; set; }
        [SeanField(8)] public string Ost { get; set; }

        // 9  ??
        // 10 bool somethingParticles
        [SeanField(11)] public int WorldMapX { get; set; }
        [SeanField(12)] public int WorldMapY { get; set; }

        [SeanField(13)] public string AreaMap { get; set; }
        [SeanField(14)] public bool OnWorldMap { get; set; }

        [SeanField(15)] public string Scale8 { get; set; }
        [SeanField(16)] public string Scale12 { get; set; }
        [SeanField(17)] public string Scale16 { get; set; }

        // 18 bool somethingEntity
        // 19 areaMapScale
        // 20 ??
        // 20 packageId
        // 21 widthScale
        // 22 heightScale
        // 23 ??
        // 24 ??
    }
}