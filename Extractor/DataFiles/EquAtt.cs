﻿using System;

namespace Extractor {
    public enum EquipType {
        Head = 1,
        Eyes = 2,
        Mouth = 3,
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
        [SeanField(7)] public int DefenseValue { get; set; }
        [SeanField(8)] public int CritValue { get; set; }
        [SeanField(9)] public int DodgeValue { get; set; }

        [SeanField(6)] public int CheerBase { get; set; }
        [SeanField(23)] public int CheerPlus { get; set; }
        [SeanField(24)] public int CheerMinus { get; set; }

        [SeanField(13)] public string MaleFile { get; set; }
        [SeanField(14)] public string FemaleFile { get; set; }
        [SeanField(15)] public string MaleIcon { get; set; }
        [SeanField(16)] public string FemaleIcon { get; set; }

        public int GetEntSlot() {
            return Type switch {
                EquipType.Head => 16,
                EquipType.Eyes => 15,
                EquipType.Mouth => 14,
                EquipType.Ears => 13,
                EquipType.Neck => 12,
                EquipType.Top => 5,
                EquipType.Pants => 4,
                EquipType.Shoes => 3,
                EquipType.Hands => 7,
                EquipType.AccessoryTop => 8,
                EquipType.AccessoryPants => 9,
                EquipType.AccessoryShoes => 10,
                EquipType.AccessoryHeld => 11,
                _ => throw new Exception("Unexpected type")
            };
        }
    }
}