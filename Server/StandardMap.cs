using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Extractor;
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

abstract class Instance {
    [JsonIgnore] public int Id { get; set; }

    [JsonIgnore] public virtual IEnumerable<Client> Players => Program.clients.Select(x => x.Value).Where(x => x.InGame && x.Player.CurrentMap == Id);
    [JsonIgnore] public abstract NpcData[] Npcs { get; }
    [JsonIgnore] public abstract MobData[] Mobs { get; }

    [JsonIgnore] public abstract Teleport[] Teleporters { get; }
    [JsonIgnore] public abstract Resource[] Resources { get; }
    [JsonIgnore] public abstract Checkpoint[] Checkpoints { get; }
}

class StandardMap : Instance {
    public MapList MapData;

    public Teleport[] _teleporters;
    public Resource[] _resources;
    public Checkpoint[] _checkpoints;

    public NpcData[] _npcs;
    public MobData[] _mobs;

    public override NpcData[] Npcs => _npcs;
    public override MobData[] Mobs => _mobs;

    public override Teleport[] Teleporters => _teleporters;
    public override Resource[] Resources => _resources;
    public override Checkpoint[] Checkpoints => _checkpoints;
}
