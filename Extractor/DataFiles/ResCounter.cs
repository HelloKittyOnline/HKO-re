using System;
using System.Linq;

namespace Extractor;

[SeanItem(51)]
public struct ResCounter {
    private static readonly Random rng = new();

    [SeanField(0)] public int Id { get; set; }
    [SeanField(1, 25)] public Item[] Items { get; set; }
    private int _max;

    public struct Item {
        [SeanField(0)] public int ItemId { get; set; }
        [SeanField(1)] public int Chance { get; set; }
    }

    public void Init() {
        Items = Items.Where(x => x.ItemId != 0 && x.Chance != 0).ToArray();
        _max = Items.Sum(x => x.Chance);
    }

    public int GetRandom() {
        var rand = rng.Next(_max);

        var total = 0;
        foreach(var el in Items) {
            total += el.Chance;
            if(rand < total)
                return el.ItemId;
        }

        return 0;
    }
}
