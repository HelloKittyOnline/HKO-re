namespace Extractor;

[SeanItem(18)]
public struct Teleport {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public int FromMap { get; set; }
    [SeanField(2)] public int FromX { get; set; }
    [SeanField(3)] public int FromY { get; set; }

    [SeanField(4)] public int ToMap { get; set; }
    [SeanField(5)] public int ToX { get; set; }
    [SeanField(6)] public int ToY { get; set; }

    [SeanField(7)] public string Name { get; set; }
    [SeanField(8)] public int QuestFlag { get; set; }
    [SeanField(9)] public string File { get; set; }
    [SeanField(10)] public int Rotation { get; set; }

    [SeanField(11)] public int TutorialFlag { get; set; }
    [SeanField(12)] public int DreamRoomNum { get; set; }
    [SeanField(13)] public int KeyItem { get; set; }
    [SeanField(14)] public int KeyItemCount { get; set; }
    [SeanField(15)] public int SomethingRotation { get; set; }
    [SeanField(16)] public int WarningStringId { get; set; }

    [SeanField(17)] public int EquipItem => 0;
}
