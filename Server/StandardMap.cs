using Extractor;
using Server.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Resource = Extractor.Resource;

namespace Server;

enum NpcAction {
    Shop = 1,
    FarmManagement = 2,
    FarmTeleport = 3,
    SkillMaster = 4,
    // MiniGameManager = 5, 7, 25, 26, 27
    GuildManagement = 6,
    // Greet? = 8, // prevents dialog and sends 05_07
    BreedingService = 9,
    TrainService = 10,
    GameRoomManagement = 11,
    Tutorial = 12,
    Repair = 13,
    Recharge = 14,
    Redeem = 15,
    Food4Friends_Donate = 16,
    Food4Friends_FoodPointList = 17,
    Food4Friends_Standing = 18,
    ItemMall = 19,
    PODLeaderboard_Donate = 20,
    PODLeaderboard_ItemPointList = 21,
    PODLeaderboard_Standing = 22,
    MaterialList = 23,
    OpenHomeroo = 24,
    // 25 // open http://game.sanriotown.com/hko/sso/login.php?sso_token=%s
    EarthDayRecycling = 28,
    EarthDayRecyclingList = 29,
    GoToTheSecretGarden = 30,
    BingoRoom = 31,
    EmoticonRoom = 32,
    CityCookOff_Donate = 33,
    CityCookOff_ItemPointList = 34,
    CityCookOff_Standing = 35,
    ChocolateDefense_Standing = 36,
    BirthdayEvent_Standing = 37
}

struct NpcData {
    public int Id { get; set; }
    public int MapId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; set; }

    public NpcAction Action1 { get; set; }
    public NpcAction Action2 { get; set; }
    public NpcAction Action3 { get; set; }
    public NpcAction Action4 { get; set; }

    public static NpcData[] Load(string path) {
        return JsonSerializer.Deserialize<NpcData[]>(System.IO.File.ReadAllText(path));
    }
}

class MobData : IWriteAble {
    public int Id { get; set; }

    // data
    public int MobId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public byte Direction { get; set; }
    public int Hp { get; set; }
    public int IsPet { get; set; }
    public byte State { get; set; }

    public byte Speed => 10;
    public int MaxHp => Program.mobAtts[MobId].Hp;

    private bool isRespawning = false;

    public MobData(int id, int mobId, int x, int y) {
        Id = id;
        MobId = mobId;
        X = x;
        Y = y;
        Direction = 5;
        Hp = MaxHp;
        IsPet = 0;

        State = 1;
        // 1 = normal
        // 2 = alert
        // 3 = squigly
        // 4 = sleeping
        // 5 = gone?
        // 6 = squigly also gone
        // 7 = normal
    }

    public void Write(PacketBuilder b) {
        b.WriteInt(Id);
        b.WriteInt(X);
        b.WriteInt(Y);
        b.WriteInt(MobId);

        b.WriteShort(Speed);
        b.WriteByte(Direction);
        b.WriteByte(State);

        b.WriteInt(Hp);
        b.WriteInt(MaxHp);
        b.WriteInt(IsPet);
        b.WriteInt(X); // moving?
        b.WriteInt(Y); // moving?
    }

    public async void QueueRespawn(Instance map) {
        if(isRespawning)
            return;

        isRespawning = true;

        // TODO: actual respawn time?
        await Task.Delay(10 * 1000);
        Hp = MaxHp;
        State = 1;
        isRespawning = false;

        try {
            Battle.SendMobState(map.Players, this);
        } catch { }
    }
}

abstract class Instance {
    [JsonIgnore] public int Id { get; set; }

    [JsonIgnore] public virtual IEnumerable<Client> Players => Program.clients.Where(x => x.InGame && x.Player.CurrentMap == Id);
    [JsonIgnore] public abstract IReadOnlyCollection<NpcData> Npcs { get; }
    [JsonIgnore] public abstract IReadOnlyCollection<MobData> Mobs { get; }

    [JsonIgnore] public abstract IReadOnlyCollection<Teleport> Teleporters { get; }
    [JsonIgnore] public abstract IReadOnlyCollection<Resource> Resources { get; }
    [JsonIgnore] public abstract IReadOnlyCollection<Checkpoint> Checkpoints { get; }
}

class StandardMap : Instance {
    public MapList MapData;

    public Teleport[] _teleporters;
    public Resource[] _resources;
    public Checkpoint[] _checkpoints;

    public NpcData[] _npcs;
    public MobData[] _mobs;

    public override IReadOnlyCollection<NpcData> Npcs => _npcs;
    public override IReadOnlyCollection<MobData> Mobs => _mobs;

    public override IReadOnlyCollection<Teleport> Teleporters => _teleporters;
    public override IReadOnlyCollection<Extractor.Resource> Resources => _resources;
    public override IReadOnlyCollection<Checkpoint> Checkpoints => _checkpoints;
}

class DreamRoom : StandardMap {
    public override IEnumerable<Client> Players { get; } = Enumerable.Empty<Client>();
}
