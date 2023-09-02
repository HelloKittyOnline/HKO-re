using Extractor;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server;

struct FurnitureItem : IWriteAble {
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public byte Rotation { get; set; }
    public byte State { get; set; }
    public byte Floor { get; set; }

    public void Write(ref PacketBuilder b) {
        if(State == 0)
            State = 1;

        b.WriteInt(Id);
        b.WriteInt(0); // idk
        b.WriteInt(0); // idk
        b.WriteInt(X);
        b.WriteInt(Y);
        b.WriteByte(Rotation);
        b.WriteByte(State);
        b.WriteByte(Floor);
        b.WriteByte(0); // unused?
    }
}

class HouseFloor : Instance {
    public int ViewId { get; set; } = 20;
    public int FloorId { get; set; } = 16;
    public int WallId { get; set; } = 8;
    public int WindowId { get; set; } = 12;

    [JsonIgnore] public int Number => Id % 10;

    [JsonIgnore] public House House { get; set; }

    [JsonIgnore] public override IReadOnlyCollection<NpcData> Npcs => Array.Empty<NpcData>();
    [JsonIgnore] public override IReadOnlyCollection<MobData> Mobs => Array.Empty<MobData>();
    [JsonIgnore]
    public override IReadOnlyCollection<Teleport> Teleporters {
        get {
            if(House.Data.Level == 1) {
                return Array.Empty<Teleport>();
            }

            const int n = 352 + 10;

            var tel = new List<Teleport>();

            if(Number < House.Data.Level) { // to next room
                tel.Add(new() {
                    Id = n,
                    FromX = 570,
                    FromY = 85,
                    Rotation = 1
                });
            }

            if(Number != 1) { // to prev room
                tel.Add(new() {
                    Id = n - 1,
                    FromX = 80,
                    FromY = 442,
                    Rotation = 5
                });
            }

            return tel;
        }
    }

    [JsonIgnore] public override IReadOnlyCollection<Resource> Resources => Array.Empty<Resource>();
    [JsonIgnore] public override IReadOnlyCollection<Checkpoint> Checkpoints => Array.Empty<Checkpoint>();

    public void Reset() {
        ViewId = 20;
        FloorId = 16;
        WallId = 8;
        WindowId = 12;
    }
}

class House {
    public string Name { get; set; } = string.Empty;
    public int BuildingPermit { get; set; }
    public ushort HouseState { get; set; } // 1 = collect material / 2 = play minigame / 3 = idle build / 4 = done
    public byte MinigameStage { get; set; }
    public int BuildProgress { get; set; }
    public int[] BuildingItems { get; set; } = new int[6];

    public HouseFloor Floor0 { get; set; } = new();
    public HouseFloor Floor1 { get; set; } = new();
    public HouseFloor Floor2 { get; set; } = new();
    public Dictionary<int, FurnitureItem> Furniture { get; set; } = new();

    [JsonIgnore] public ushort HouseId => (ushort)Program.items[BuildingPermit].SubId;
    [JsonIgnore] public BuildingAgreement Data => Program.buildings[HouseId];

    public void Init(Client client) {
        Floor0.Id = 30001 + client.Id * 10;
        Floor1.Id = 30002 + client.Id * 10;
        Floor2.Id = 30003 + client.Id * 10;

        Floor0.House = this;
        Floor1.House = this;
        Floor2.House = this;
    }

    public void Delete() {
        BuildingPermit = 0;
        HouseState = 0;
        MinigameStage = 0;
        BuildProgress = 0;
        BuildingItems.AsSpan().Clear();
        Furniture.Clear();

        Floor0.Reset();
        Floor1.Reset();
        Floor2.Reset();
    }

    public int AddFurniture(FurnitureItem item) {
        int i = 0;
        // find empty index
        while(Furniture.ContainsKey(i)) {
            i++;
        }
        Furniture[i] = item;
        return i;
    }
}
