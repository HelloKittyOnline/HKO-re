using System.Diagnostics;

namespace Extractor {
    public enum ItemType {
        CONSUMABLE_ITEM = 1,
        PESTICIDE = 2,
        FERTILIZER = 3,
        SEED = 4,
        GROWTH_BOOSTER = 5,
        TOOL = 6,
        EQUIPMENT = 7,
        FARM_ITEM_1 = 8,
        FARM_ITEM_2 = 9,
        ITEM_GUIDE = 10,
        PET_FOOD = 11,
        CARD = 12,
        BUILDING_PERMIT = 14,
        FURNITURE = 15,
        FIREWORKS = 16,
    };

    public struct ItemAtt {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Price { get; set; }
        public int SubId { get; set; }
        public int StackLimit { get; set; }

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
                    Type = contents.Items[i, 2],
                    SubId = contents.Items[i, 3],
                    Price = contents.Items[i, 5],
                    StackLimit = contents.Items[i, 10],
                    Icon = contents.GetString(i, 12),
                    Description = contents.GetString(i, 13),
                    CardImage = contents.GetString(i, 14)
                    // 21 always 0
                };
            }

            return items;
        }
    }
}