namespace Extractor;

[SeanItem(27)]
public struct PetInitData {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }

    [SeanField(2)] public int Unknown2 { get; set; }
    [SeanField(3)] public int Hp { get; set; }
    [SeanField(4)] public int Stamina { get; set; }
    [SeanField(5)] public int Attack { get; set; }
    [SeanField(6)] public int Defense { get; set; }
    [SeanField(7)] public int CritChance { get; set; }
    [SeanField(8)] public int DodgeChance { get; set; }
    [SeanField(9)] public int InvSize { get; set; }
    [SeanField(10)] public int Unknown10 => 0;
    [SeanField(11)] public int Unknown11 => 0;
    [SeanField(12)] public int Unknown12 { get; set; }
    [SeanField(13)] public int Unknown13 { get; set; }
    [SeanField(14)] public int Unknown14 { get; set; }
    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }
    [SeanField(17)] public int Unknown17 { get; set; }
    [SeanField(18)] public int Unknown18 { get; set; }
    [SeanField(19)] public int Unknown19 { get; set; }
    [SeanField(20)] public int parent1_id { get; set; }
    [SeanField(21)] public int parent1_lvl { get; set; }
    [SeanField(22)] public int parent2_id { get; set; }
    [SeanField(23)] public int parent2_lvl { get; set; }
    [SeanField(24)] public int breed_price { get; set; }
    [SeanField(25)] public int Unknown25 => 0;
    [SeanField(26)] public int Unknown26 { get; set; }
}
