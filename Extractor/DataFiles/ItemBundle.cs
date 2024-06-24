namespace Extractor;

[SeanItem(21)]
readonly struct ItemBundle {
    [SeanField(0)] public int Id { get; init; }
    [SeanField(1, 10)] public Effect[] Effects { get; init; }

    public readonly struct Effect {
        [SeanField(0)] public int A { get; init; }
        [SeanField(1)] public int B { get; init; }
    }
}
