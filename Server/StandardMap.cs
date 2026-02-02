using System;
using Extractor;
using Server.Protocols;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly Random _random = new();

    public int Id { get; set; }

    // data
    public int MobId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public byte Direction { get; set; }
    public int Hp { get; set; }
    public int IsPet { get; set; }
    public byte State { get; set; }

    // Spawn position (for wandering bounds)
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }

    // Movement target
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public bool IsMoving { get; set; }

    // Timing
    public long NextMoveTime { get; set; }
    public long MoveEndTime { get; set; }

    public short Speed => 10;
    public int MaxHp => Data.Hp;

    // Wandering configuration
    public const int WanderRadius = 150;      // Max distance from spawn
    public const int MinIdleTime = 3000;      // Min ms between moves
    public const int MaxIdleTime = 8000;      // Max ms between moves
    public const int MoveSpeed = 10;          // Movement speed

    // Aggro configuration
    public const int AggroRadius = 200;       // Detection radius for aggressive mobs
    public const int ChaseRadius = 400;       // Max chase distance from spawn before returning
    public const int AttackRange = 50;        // Range at which mob can attack
    public const int AttackCooldown = 2000;   // Ms between mob attacks

    // Aggro state
    public Client AggroTarget { get; set; }   // Currently aggro'd player
    public long LastAttackTime { get; set; }  // Last time mob attacked
    public bool IsAggressive => Data.Aggressive;

    public MobAtt Data => Program.mobAtts[MobId];

    private bool isRespawning = false;

    public MobData(int id, int mobId, int x, int y) {
        Id = id;
        MobId = mobId;
        X = x;
        Y = y;
        SpawnX = x;
        SpawnY = y;
        TargetX = x;
        TargetY = y;
        Direction = 5;
        Hp = MaxHp;
        IsPet = 0;
        IsMoving = false;

        State = 1;
        // 1 = normal
        // 2 = alert
        // 3 = squigly
        // 4 = sleeping/dead
        // 5 = gone?
        // 6 = squigly also gone
        // 7 = normal

        // Set initial next move time
        NextMoveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _random.Next(MinIdleTime, MaxIdleTime);
    }

    public void Write(ref PacketBuilder b) {
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
        b.WriteInt(TargetX); // target X for movement interpolation
        b.WriteInt(TargetY); // target Y for movement interpolation
    }

    /// <summary>
    /// Update mob movement. Returns true if mob started moving and needs to broadcast.
    /// </summary>
    public bool UpdateMovement() {
        if (Hp <= 0 || State == 4) return false; // Dead mobs don't move

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // If currently moving, check if arrived at destination
        if (IsMoving) {
            if (now >= MoveEndTime) {
                // Arrived at destination
                X = TargetX;
                Y = TargetY;
                IsMoving = false;

                // Schedule next move (shorter if chasing)
                if (AggroTarget != null) {
                    NextMoveTime = now + 100; // Quick repositioning during chase
                } else {
                    NextMoveTime = now + _random.Next(MinIdleTime, MaxIdleTime);
                }
            }
            return false;
        }

        // Check if it's time to start a new move
        if (now >= NextMoveTime) {
            // If we have an aggro target, chase them
            if (AggroTarget != null) {
                return StartChase();
            }
            return StartWander();
        }

        return false;
    }

    /// <summary>
    /// Update aggro state for this mob. Called from aggro thread with player list.
    /// Returns (shouldAttack, target) if mob should attack a player.
    /// </summary>
    public (bool shouldAttack, Client target) UpdateAggro(IEnumerable<Client> players) {
        if (Hp <= 0 || State == 4) {
            AggroTarget = null;
            return (false, null);
        }

        if (!IsAggressive) {
            return (false, null);
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Check if current target is still valid
        if (AggroTarget != null) {
            if (!AggroTarget.InGame || AggroTarget.Player.CurrentMap != GetMapId(players)) {
                // Target left, clear aggro
                AggroTarget = null;
                State = 1; // Back to normal state
            } else {
                // Check if target is too far from spawn (mob gives up)
                var distFromSpawn = GetDistance(X, Y, SpawnX, SpawnY);
                if (distFromSpawn > ChaseRadius) {
                    AggroTarget = null;
                    State = 1;
                    // Return to spawn
                    TargetX = SpawnX;
                    TargetY = SpawnY;
                    IsMoving = true;
                    MoveEndTime = now + (int)(distFromSpawn / Speed * 100);
                }
            }
        }

        // Look for new target if we don't have one
        if (AggroTarget == null) {
            foreach (var player in players) {
                if (!player.InGame || player.Player.Hp <= 0) continue;

                var dist = GetDistance(X, Y, player.Player.PositionX, player.Player.PositionY);
                if (dist <= AggroRadius) {
                    AggroTarget = player;
                    State = 2; // Alert state
                    break;
                }
            }
        }

        // Check if we should attack
        if (AggroTarget != null && now >= LastAttackTime + AttackCooldown) {
            var dist = GetDistance(X, Y, AggroTarget.Player.PositionX, AggroTarget.Player.PositionY);
            if (dist <= AttackRange) {
                LastAttackTime = now;
                return (true, AggroTarget);
            }
        }

        return (false, null);
    }

    private int GetMapId(IEnumerable<Client> players) {
        var first = players.FirstOrDefault();
        return first?.Player?.CurrentMap ?? 0;
    }

    private static double GetDistance(int x1, int y1, int x2, int y2) {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Start chasing the aggro target.
    /// </summary>
    private bool StartChase() {
        if (AggroTarget == null || !AggroTarget.InGame) {
            AggroTarget = null;
            return false;
        }

        // Move toward target
        var targetX = AggroTarget.Player.PositionX;
        var targetY = AggroTarget.Player.PositionY;

        // Don't move if already in attack range
        var dist = GetDistance(X, Y, targetX, targetY);
        if (dist <= AttackRange) {
            return false;
        }

        // Move closer but not on top of target
        var dx = targetX - X;
        var dy = targetY - Y;
        var moveRatio = Math.Min(1.0, (dist - AttackRange / 2) / dist);

        TargetX = X + (int)(dx * moveRatio);
        TargetY = Y + (int)(dy * moveRatio);

        Direction = CalculateDirection(dx, dy);

        var moveDist = GetDistance(X, Y, TargetX, TargetY);
        var moveDuration = (int)(moveDist / Speed * 100);

        IsMoving = true;
        MoveEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + moveDuration;

        return true;
    }

    /// <summary>
    /// Start wandering to a random position within radius of spawn point.
    /// </summary>
    private bool StartWander() {
        // Pick a random angle and distance
        var angle = _random.NextDouble() * 2 * Math.PI;
        var distance = _random.Next(30, WanderRadius);

        // Calculate target position
        var newX = SpawnX + (int)(Math.Cos(angle) * distance);
        var newY = SpawnY + (int)(Math.Sin(angle) * distance);

        // Clamp to reasonable bounds (prevent going negative)
        newX = Math.Max(50, newX);
        newY = Math.Max(50, newY);

        TargetX = newX;
        TargetY = newY;

        // Calculate direction (8 directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW)
        var dx = TargetX - X;
        var dy = TargetY - Y;
        Direction = CalculateDirection(dx, dy);

        // Calculate move duration based on distance and speed
        var moveDistance = Math.Sqrt(dx * dx + dy * dy);
        var moveDuration = (int)(moveDistance / Speed * 100); // ms

        IsMoving = true;
        MoveEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + moveDuration;

        return true;
    }

    /// <summary>
    /// Calculate direction byte from delta X/Y (8 directions)
    /// </summary>
    private static byte CalculateDirection(int dx, int dy) {
        // Normalize to angle
        var angle = Math.Atan2(dy, dx);
        // Convert to 0-7 direction (0=E, going counter-clockwise)
        // HKO uses: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        var dir = (int)Math.Round((angle + Math.PI) / (Math.PI / 4)) % 8;
        // Remap from math angle to game direction
        return (byte)((6 - dir + 8) % 8);
    }

    public async void QueueRespawn(Instance map) {
        if(isRespawning)
            return;

        isRespawning = true;

        // Clear aggro immediately
        AggroTarget = null;

        // Respawn after 10 seconds
        await Task.Delay(10 * 1000);

        // Reset to spawn position
        X = SpawnX;
        Y = SpawnY;
        TargetX = SpawnX;
        TargetY = SpawnY;
        Hp = MaxHp;
        State = 1;
        IsMoving = false;
        AggroTarget = null;
        LastAttackTime = 0;
        NextMoveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _random.Next(MinIdleTime, MaxIdleTime);
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
