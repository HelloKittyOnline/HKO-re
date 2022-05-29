using System;
using System.Collections.Generic;

namespace Extractor {
    public struct ResCounter {
        private static readonly Random rng = new Random();

        public struct Item {
            public int ItemId { get; set; }
            public int Chance { get; set; }
        }

        public int Id { get; set; }
        public Item[] Items { get; set; }

        public static ResCounter[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new ResCounter[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                var dat = new List<Item>(10);

                for(int j = 0; j < 10; j++) {
                    var id = contents.Items[i, j * 2 + 1];
                    if(id == 0)
                        break;
                    dat.Add(new Item {
                        ItemId = id,
                        Chance = contents.Items[i, j * 2 + 2]
                    });
                }

                items[i] = new ResCounter {
                    Id = contents.Items[i, 0],
                    Items = dat.ToArray()
                };
            }

            return items;
        }

        public int GetRandom() {
            var rand = rng.Next(10000);

            var total = 0;
            foreach(var el in Items) {
                total += el.Chance;
                if(rand < total)
                    return el.ItemId;
            }

            return -1;
        }
    }
}