using System;
using System.Text.Json.Serialization;

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
    // 13
    Building_Permit = 14,
    Furniture = 15,
    Fireworks = 16,
    // 17
    Farm_Certificate = 18,
    Watering_Can = 19,
    Pet_Bag = 20,
    // 21
    // 22
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

[Flags]
public enum TransferFlag {
    NON_TRANSFERABLE_TO_PLAYER = 1,
    NON_TRANSFERABLE_TO_MERCHANT = 2,
    NON_TRANSFERABLE = NON_TRANSFERABLE_TO_PLAYER | NON_TRANSFERABLE_TO_MERCHANT,
    NON_DROPPABLE = 4,
    NON_DROPPABLE_NON_TRANSFERRABLE = NON_DROPPABLE | NON_TRANSFERABLE,
}

[SeanItem(22)]
public struct ItemAtt {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public string Name { get; set; }
    [SeanField(2)] public ItemType Type { get; set; }
    [SeanField(3)] public int SubId { get; set; }
    [SeanField(4)] public int StartQuestFlag { get; set; }
    [SeanField(5)] public int Price { get; set; }

    [SeanField(6)] public int Unknown6 { get; set; }
    [SeanField(7)] public TransferFlag Transferable { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }
    [SeanField(9)] public bool Unknown9 { get; set; }

    [SeanField(10)] public int _stackLimit { get; set; }
    [JsonIgnore] public int StackLimit => (Type is ItemType.Equipment or ItemType.Tool) ? 1 : _stackLimit;
    [SeanField(11)] public int Level { get; set; }

    [SeanField(12)] public string Icon { get; set; }
    [SeanField(13)] public string Description { get; set; }
    [SeanField(14)] public string CardImage { get; set; }

    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int LifeTime { get; set; } // in minutes
    [SeanField(17)] public int TimeoutItemId { get; set; } // resulting item after LifeTime runs out
    [SeanField(18)] public int Durability { get; set; } // never fully implemented
    [SeanField(19)] public int DurabilityPriceFactor { get; set; } // Price - (maxDurability - durability) / (DurabilityPriceFactor / 10.0)
    [SeanField(20)] public int ItemQuality => 0;
    [SeanField(21)] public int Unknown21 => 0;

    public int GetCharges() {
        // is this actually ever used in game?
        if(SubId > 100 && Type is ItemType.Fertilizer or ItemType.Crop_Remover) {
            return SubId - 100;
        }
        return 0;
    }
}
