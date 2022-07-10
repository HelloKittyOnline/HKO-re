using System;
using System.Collections.Generic;

namespace Extractor {
    public struct ProdRule {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int RequiredLevel { get; set; }
        public int Count { get; set; }
        public Item[] Ingredients { get; set; }

        public struct Item {
            public int ItemId;
            public int Count;
        }

        public static ProdRule[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new ProdRule[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                var req = new List<Item>();
                for(int j = 0; j < 5; j++) {
                    var id = contents.Items[i, j * 2 + 4];
                    var count = contents.Items[i, j * 2 + 5];

                    if(id == 0)
                        continue;

                    req.Add(new Item {
                        ItemId = id,
                        Count = count,
                    });
                }

                items[i] = new ProdRule {
                    Id = contents.Items[i, 0],
                    RequiredLevel = contents.Items[i, 1],
                    ItemId = contents.Items[i, 2],
                    Count = contents.Items[i, 3],
                    Ingredients = req.ToArray()
                };
            }

            return items;
        }

        public Skill GetSkill() {
            return (Id / 512) switch {
                0 => Skill.Forging,
                1 => Skill.Forging,
                2 => Skill.Carpentry,
                3 => Skill.Carpentry,
                4 => Skill.Cooking,
                5 => Skill.Cooking,
                6 => Skill.Tailoring,
                7 => Skill.Tailoring,
                8 => Skill.Tailoring,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}