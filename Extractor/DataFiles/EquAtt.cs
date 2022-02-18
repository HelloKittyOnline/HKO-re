using System;
using System.Diagnostics;

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

    public struct EquAtt {
        public int Id { get; set; }
        public int Gender { get; set; }
        public EquipType Type { get; set; }

        public int EnergyIncrease { get; set; }
        public int ActionPoints { get; set; }
        public int DefenseValue { get; set; }
        public int CritValue { get; set; }
        public int DodgeValue { get; set; }

        public int CheerBase { get; set; }
        public int CheerPlus { get; set; }
        public int CheerMinus { get; set; }

        public string MaleFile { get; set; }
        public string FemaleFile { get; set; }
        public string MaleIcon { get; set; }
        public string FemaleIcon { get; set; }

        public static EquAtt[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new EquAtt[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                Debug.Assert(contents.Items[i, 0] == i);

                items[i] = new EquAtt {
                    Id = contents.Items[i, 0],
                    Gender = contents.Items[i, 1],
                    Type = (EquipType)contents.Items[i, 2],
                    EnergyIncrease = contents.Items[i, 4],
                    ActionPoints = contents.Items[i, 5],
                    CheerBase = contents.Items[i, 6],
                    DefenseValue = contents.Items[i, 7],
                    CritValue = contents.Items[i, 8],
                    DodgeValue = contents.Items[i, 9],
                    MaleFile = contents.GetString(i, 13),
                    FemaleFile = contents.GetString(i, 14),
                    MaleIcon = contents.GetString(i, 15),
                    FemaleIcon = contents.GetString(i, 16),
                    // 21 always 0
                    CheerPlus = contents.Items[i, 23],
                    CheerMinus = contents.Items[i, 24],
                };
            }

            return items;
        }

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