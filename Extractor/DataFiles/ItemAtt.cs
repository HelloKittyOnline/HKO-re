using System.Diagnostics;

namespace Extractor {
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

    public struct ItemAtt {
        public int Id { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int SubId { get; set; }
        public int Price { get; set; }
        public int StackLimit { get; set; }
        public int Level { get; set; }

        public string Icon { get; set; }
        public string Description { get; set; }
        public string CardImage { get; set; }

        public static ItemAtt[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new ItemAtt[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                Debug.Assert(contents.Items[i, 0] == i);

                items[i] = new ItemAtt {
                    Id = contents.Items[i, 0],
                    Name = contents.GetString(i, 1),
                    Type = (ItemType)contents.Items[i, 2],
                    SubId = contents.Items[i, 3],
                    Price = contents.Items[i, 5],
                    StackLimit = contents.Items[i, 10],
                    Level = contents.Items[i, 11],
                    Icon = contents.GetString(i, 12),
                    Description = contents.GetString(i, 13),
                    CardImage = contents.GetString(i, 14)
                };
            }

            return items;
        }
    }
}