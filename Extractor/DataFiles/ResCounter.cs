using System;
using System.Collections.Generic;

namespace Extractor {
    public struct ResCounter {
        private static readonly Random rng = new();

        public struct Item {
            public int ItemId { get; set; }
            public int Chance { get; set; }
        }

        public int Id { get; set; }
        public Item[] Items { get; set; }
        private int Max;

        public static ResCounter[] Load(byte[] data) {
            var contents = new SeanDatabase(data);

            var items = new ResCounter[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                var dat = new List<Item>(10);
                int max = 0;

                for(int j = 0; j < 10; j++) {
                    var id = contents.Items[i, j * 2 + 1];
                    if(id == 0)
                        break;

                    var chance = contents.Items[i, j * 2 + 2];
                    max += chance;

                    dat.Add(new Item {
                        ItemId = id,
                        Chance = chance
                    });
                }

                items[i] = new ResCounter {
                    Id = contents.Items[i, 0],
                    Items = dat.ToArray(),
                    Max = max
                };
            }

            return items;
        }

        public int GetRandom() {
            var rand = rng.Next(Max);

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