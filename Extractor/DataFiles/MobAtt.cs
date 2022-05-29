namespace Extractor {
    [SeanItem(26)]
    public class MobAtt {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public string Name { get; set; }
        [SeanField(2)] public int Level { get; set; }
        [SeanField(9)] public int LootTable { get; set; }
        [SeanField(3)] public int Hp { get; set; }

        [SeanField(14)] public string File { get; set; }
    }
}