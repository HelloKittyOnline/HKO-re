namespace Extractor;

public enum FurnitureType {
    Object = 1,
    Room = 2
}

public enum FurniturePosition {
    Floor = 1,
    Wall = 2,
    Window = 3,
    Outside = 4
}

public struct OffsetInfo {
    [SeanField(0)] public int X { get; set; }
    [SeanField(1)] public int Y { get; set; }
}

public struct Idk {
    [SeanField(0)] public int Unknown0 { get; set; }
    [SeanField(1)] public int Unknown1 { get; set; }
    [SeanField(2)] public int Unknown2 { get; set; }
    [SeanField(3)] public int Unknown3 { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public int Unknown5 { get; set; }
    [SeanField(6)] public int Unknown6 { get; set; }
    [SeanField(7)] public int Unknown7 { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }
    [SeanField(9)] public int Unknown9 { get; set; }
    [SeanField(10)] public int Unknown10 { get; set; }
    [SeanField(11)] public int Unknown11 { get; set; }
    [SeanField(12)] public int Unknown12 { get; set; }
    [SeanField(13)] public int Unknown13 { get; set; }
    [SeanField(14)] public int Unknown14 { get; set; }
    [SeanField(15)] public int Unknown15 { get; set; }
    [SeanField(16)] public int Unknown16 { get; set; }
}

[SeanItem(86)]
public struct Furniture {
    [SeanField(0)] public int Id { get; set; }

    [SeanField(1)] public FurnitureType Type { get; set; }
    [SeanField(2)] public FurniturePosition Position { get; set; }
    [SeanField(3)] public int Unknown3 { get; set; }
    [SeanField(4)] public int Unknown4 { get; set; }
    [SeanField(5)] public string Texture { get; set; }

    [SeanField(6)] public int Height { get; set; }
    [SeanField(7)] public int Width { get; set; }
    [SeanField(8)] public int Unknown8 { get; set; }
    [SeanField(9)] public int Unknown9 { get; set; }

    [SeanArray(10, 4)] 
    public Idk[] What { get; set; }

    [SeanArray(78, 4)]
    public OffsetInfo[] Offsets { get; set; }

    public void AdjustValues() {
        for(int i = 0; i < 4; i++) {
            What[i].Unknown1 -= 400;
            What[i].Unknown2 -= 500;

            What[i].Unknown3 -= 400;
            What[i].Unknown4 -= 500;

            What[i].Unknown5 -= 400;
            What[i].Unknown6 -= 500;

            What[i].Unknown7 -= 400;
            What[i].Unknown8 -= 500;

            What[i].Unknown9 -= 400;
            What[i].Unknown10 -= 500;

            What[i].Unknown11 -= 400;
            What[i].Unknown12 -= 500;

            What[i].Unknown13 -= 400;
            What[i].Unknown14 -= 500;

            What[i].Unknown15 -= 400;
            What[i].Unknown16 -= 500;
        }
    }
}
