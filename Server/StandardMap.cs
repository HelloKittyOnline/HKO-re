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

struct NpcData {
    public int Id { get; set; }
    public int MapId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; set; }

    public int Action1 { get; set; }
    public int Action2 { get; set; }
    public int Action3 { get; set; }
    public int Action4 { get; set; }

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

    public MobData(int id, int mobId, int x, int y) {
        Id = id;
        MobId = mobId;
        X = x;
        Y = y;
        Direction = 5;
        Hp = MaxHp;
        IsPet = 0;

        State = 1;
        // 0 = normal
        // 1 = normal
        // 2 = alert
        // 3 = squigly
        // 4 = sleeping
        // 5 = gone?
        // 6 = squigly also gone
        // 7 = normal
    }

    private Task respawnTask;

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

    public void QueueRespawn(Instance map) {
        respawnTask ??= Task.Run(() => {
            // TODO: actual respawn time?
            Thread.Sleep(10 * 1000);
            Hp = MaxHp;
            State = 1;

            Battle.SendMobState(map.Players, this);

            respawnTask = null;
        });
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
