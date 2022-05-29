namespace Extractor {
    [SeanItem(17)]
    public struct Resource {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public int MapId { get; set; }
        [SeanField(2)] public int X { get; set; }
        [SeanField(3)] public int Y { get; set; }

        [SeanField(4)] public short NameId { get; set; }
        [SeanField(5)] public short Level { get; set; }

        [SeanField(6)] public int LootTable { get; set; }

        [SeanField(7)] public int Tool1 { get; set; }
        [SeanField(8)] public int Tool2 { get; set; }

        [SeanField(9)] public byte Type1 { get; set; }
        [SeanField(13)] public byte Type2 { get; set; }
    }
}