namespace Extractor;

public enum ItemType {
    Consumable_Item = 1,
    Pesticide = 2, // unused
    Fertilizer = 3,
    Seed = 4,
    Growth_Booster = 5, // unused
    Tool = 6,
    Equipment = 7,
    Farm_Item_1 = 8,
    Farm_Item_2 = 9, // unused
    Item_Guide = 10,
    Pet_Food = 11,
    Card = 12,
    Building_Permit = 14,
    Furniture = 15,
    Fireworks = 16,
    Farm_Certificate = 18,
    Watering_Can = 19,
    Pet_Bag = 20,
    // 23
    // 24
    Bingo_Card = 25,
    // 26
    Guild_Petition_Form = 27,
    Bundle_Item = 28,
    // 29
    // 30
    Farm_Draw_Pattern = 31,
    Crop_Remover = 32,
    Teleport_Tool = 33
};

[SeanItem(22)]
public struct ItemAtt {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public ItemType Type { get; set; }
    [SeanField(3)] public int SubId { get; set; }
    [SeanField(4)] public int RelatedQuest { get; set; }
    [SeanField(5)] public int Price { get; set; }

    [SeanField(6)] public int Unknown6 { get; set; }
    [SeanField(7)] public int Unknown7 { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }
    [SeanField(9)] public int Unknown9 { get; set; }

    [SeanField(10)] public int StackLimit { get; set; }
    [SeanField(11)] public int Level { get; set; }

    [SeanField(12)] public string Icon { get; set; }
    [SeanField(13)] public string Description { get; set; }
    [SeanField(14)] public string CardImage { get; set; }

    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }
    [SeanField(17)] public int Unknown17 { get; set; }
    [SeanField(18)] public int Unknown18 { get; set; }
    [SeanField(19)] public int Unknown19 { get; set; }
    [SeanField(20)] public int Unknown20 { get; set; }
    [SeanField(21)] public int Unknown21 { get; set; }
}
