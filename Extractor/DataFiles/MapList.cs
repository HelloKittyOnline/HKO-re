namespace Extractor;

[SeanItem(25)]
public struct MapList {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int FarmX { get; set; }
    [SeanField(2)] public int FarmY { get; set; }
    [SeanField(3)] public int SpawnX { get; set; }
    [SeanField(4)] public int SpawnY { get; set; }

    [SeanField(5)] public int Idk5 { get; set; }

    [SeanField(6)] public string Name { get; set; }
    [SeanField(7)] public string File { get; set; }
    [SeanField(8)] public string Ost { get; set; }

    [SeanField(9)] public int Idk9 => 0;
    [SeanField(10)] public bool somethingParticles { get; set; }

    [SeanField(11)] public int WorldMapX { get; set; }
    [SeanField(12)] public int WorldMapY { get; set; }

    [SeanField(13)] public string AreaMap { get; set; }
    [SeanField(14)] public bool OnWorldMap { get; set; }

    [SeanField(15)] public string Scale8 { get; set; }
    [SeanField(16)] public string Scale12 { get; set; }
    [SeanField(17)] public string Scale16 { get; set; }

    [SeanField(18)] public bool somethingEntity { get; set; }
    [SeanField(19)] public int areaMapScale { get; set; }
    [SeanField(20)] public int Idk20 { get; set; }

    [SeanField(21)] public int packageId { get; set; }
    [SeanField(22)] public int widthScale { get; set; }
    [SeanField(23)] public int heightScale { get; set; }
    [SeanField(24)] public int Idk24 => 0;
}
