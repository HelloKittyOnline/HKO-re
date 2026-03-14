namespace Extractor;

struct CapsuleMachineCapsule {
    [SeanField(0)] public string Name { get; set; }
    [SeanField(1)] public int Texture { get; set; }
    [SeanField(2)] public int Size { get; set; }
    [SeanField(3)] public int Unknown3 { get; set; } // Relative Chance?
    [SeanField(4)] public int Unknown4 { get; set; } // Number of rolls? 0 or 1
}

[SeanItem(114)]
struct CapsuleMachine {
    [SeanField(0)] public int Id { get; set; }
    [SeanField(1)] public int MapId { get; set; }
    [SeanField(2)] public int PosX { get; set; }
    [SeanField(3)] public int PosY { get; set; }
    [SeanField(4)] public int AnimationFrame { get; set; }
    [SeanField(5)] public string MachineSprite { get; set; }
    [SeanField(6)] public string Name { get; set; }
    [SeanField(7)] public int Flags { get; set; } // UFO | Capsule,  
    [SeanField(8)] public int TokenType { get; set; }
    [SeanField(9)] public int NumTokens { get; set; }
    [SeanField(10)] public int ConsolationTickets { get; set; }
    [SeanField(11)] public int Unknown11 { get; set; }
    [SeanField(12)] public int Unknown12 { get; set; }
    [SeanField(13)] public int NumCap { get; set; }

    [SeanField(14, 20)] public CapsuleMachineCapsule[] Capsules { get; set; }
}
