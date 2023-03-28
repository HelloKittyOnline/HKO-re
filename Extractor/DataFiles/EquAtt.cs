using System;

namespace Extractor;

public enum EquipType {
    Head = 1,
    Eyes = 2,
    Mouth = 3, // lower face
    Ears = 4,
    Neck = 5,
    Top = 6,
    Pants = 7,
    Shoes = 8,
    Hands = 9,
    AccessoryTop = 10,
    AccessoryPants = 11,
    AccessoryShoes = 12,
    AccessoryHeld = 13,
    Makeup = 21,
    Hairstyle = 22,
    Tattoo = 23,
    SkinTone = 24,
    FacialFeatures = 25
}

[SeanItem(26)]
public struct EquAtt {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int Gender { get; set; }
    [SeanField(2)] public EquipType Type { get; set; }

    [SeanField(4)] public int EnergyIncrease { get; set; }
    [SeanField(5)] public int ActionPoints { get; set; }
    [SeanField(6)] public int EnergyDrain { get; set; }
    [SeanField(7)] public int DefenseValue { get; set; }
    [SeanField(8)] public int CritValue { get; set; } // in 1 -> 0.01% increments
    [SeanField(9)] public int DodgeValue { get; set; }

    [SeanField(13)] public string MaleFile { get; set; }
    [SeanField(14)] public string FemaleFile { get; set; }
    [SeanField(15)] public string MaleIcon { get; set; }
    [SeanField(16)] public string FemaleIcon { get; set; }

    [SeanField(23)] public int CheerPlus { get; set; }
    [SeanField(24)] public int CheerMinus { get; set; }

    [SeanField(3)] public int Unknown3 { get; set; }
    [SeanField(10)] public int Unknown10 { get; set; }
    [SeanField(11)] public int Unknown11 { get; set; }
    [SeanField(12)] public int Unknown12 { get; set; }
    [SeanField(17)] public int Unknown17 { get; set; }
    [SeanField(18)] public int Unknown18 { get; set; }
    [SeanField(19)] public int Unknown19 { get; set; }
    [SeanField(20)] public int Unknown20 => 0;
    [SeanField(21)] public int Unknown21 => 0;
    [SeanField(22)] public int Unknown22 { get; set; }
    [SeanField(25)] public int Unknown25 { get; set; }

    public int GetEntSlot() {
        // 0 - skin
        // 2 - face
        // 3 - shoes
        // 4 - pants
        // 5 - shirt
        // 6 - hair
        // 14,15,16,17 - does not work

        return Type switch {
            EquipType.Shoes => 3,
            EquipType.Pants => 4,
            EquipType.Top => 5,
            EquipType.Hands => 7,

            EquipType.AccessoryShoes => 3,
            EquipType.AccessoryPants => 4,
            EquipType.AccessoryTop => 5,
            EquipType.AccessoryHeld => 7,

            EquipType.Head => 8,
            EquipType.Eyes => 10,
            EquipType.Mouth => 9,
            EquipType.Ears => 11,
            EquipType.Neck => 13,

            EquipType.Makeup => 1,
            EquipType.Hairstyle => 1,
            EquipType.Tattoo => 1,
            EquipType.SkinTone => 1,
            EquipType.FacialFeatures => 1,
            _ => throw new Exception("Unexpected type")
        };
    }
}
