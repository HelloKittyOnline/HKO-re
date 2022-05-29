namespace Extractor {
    [SeanItem(7)]
    public class NPCName {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public string Name { get; set; }
        [SeanField(2)] public string File { get; set; }

        [SeanField(3)] public int MapId { get; set; }
        [SeanField(4)] public int X { get; set; }
        [SeanField(5)] public int Y { get; set; }
        [SeanField(6)] public int Rotation { get; set; }
    }
}