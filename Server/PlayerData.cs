using Extractor;
using Server.Protocols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Server;

struct InventoryItem : IWriteAble {
    public int Id { get; set; }
    public byte Count { get; set; }
    // public byte Durability { get; set; }
    public byte Charges { get; set; }

    [JsonIgnore]
    public ItemAtt Data => Program.items[Id];

    public void Write(ref PacketBuilder w) {
        w.WriteInt(Id); // id

        w.WriteByte(Count); // count
        w.WriteByte(0); // durability - unused
        w.WriteByte(Charges); // charges
        w.WriteByte(0); // unused?

        w.WriteByte(0); // idk
        w.WriteByte(0); // idk
        w.WriteByte(0); // idk
        w.WriteByte(0); // flags
        // f & 1 = 
        // f & 2 = 
        // f & 4 = scrapped?
    }
}

enum QuestStatus {
    None = 0,
    Running = 1,
    Done = 2
}

class PlayerData {
    public int CurrentMap { get; set; } = 1; // Dream Room 1
    public int ReturnMap = 8;

    [JsonIgnore] public Instance Map => Program.maps[CurrentMap];

    [JsonIgnore]
    public int MapType {
        get {
            int mapId = CurrentMap;

            if(mapId < 30000) {
                return 1; // normal
            }

            if(59900 <= mapId && mapId < 59950) {
                return 7; // Pinata Map
            }

            if(55000 < mapId && mapId <= 55500) {
                return 5;
            }

            if(55500 < mapId) {
                return 6; // time out?
            }

            switch(mapId % 10) {
                case 0:
                    return 3; // farm
                case 1:
                case 2:
                case 3:
                    return 4; // house
                case 7:
                    return 8; // Dream Room 4
                case 8:
                    return 9; // Green House
            }

            return 0;
        }
    }

    public int PositionX { get; set; } = 352;
    public int PositionY { get; set; } = 688;
    public byte Rotation { get; set; }
    [JsonIgnore]
    public byte Speed => 200;

    // animation state
    [JsonIgnore]
    public byte State { get; set; }
    // status icon
    [JsonIgnore]
    public int Status { get; set; }

    public string Name { get; set; }
    public byte Gender { get; set; }
    public byte BloodType { get; set; }
    public byte BirthMonth { get; set; }
    public byte BirthDay { get; set; }

    public int[] BaseEntities { get; set; }
    [JsonIgnore]
    public int[] DisplayEntities { get; private set; }

    public int Money { get; set; }
    public int NormalTokens { get; set; }
    public int SpecialTokens { get; set; }
    public int Tickets { get; set; }

    [JsonIgnore] public int Hp { get; set; }
    [JsonIgnore] public int MaxHp { get; set; }
    [JsonIgnore] public int Sta { get; set; }
    [JsonIgnore] public int MaxSta { get; set; }
    [JsonIgnore] public int Attack { get; set; }
    [JsonIgnore] public int Defense { get; set; }
    [JsonIgnore] public ushort Crit { get; set; }
    [JsonIgnore] public ushort Dodge { get; set; }

    public short[] Levels { get; set; }
    public int[] Exp { get; set; }
    public short[] Friendship { get; set; }

    // TODO: cache active quests?
    public Dictionary<int, QuestStatus> QuestFlags { get; set; } // todo: eventually rename to globalFlags
    public Dictionary<int, int> CheckpointFlags { get; set; }
    public Dictionary<int, uint> QuestFlags1 { get; set; } // TODO: cache active quests?

    // used for encyclopedia
    public HashSet<int> Npcs { get; set; }
    public HashSet<int> Keys { get; set; }
    public HashSet<int> Dreams { get; set; }

    [JsonIgnore]
    public int InventorySize => Math.Min(50, 24 + Levels[(int)Skill.General]);
    public InventoryItem[] Inventory { get; set; }
    public InventoryItem[] Equipment { get; set; }
    public InventoryItem[] Tools { get; set; }

    public int[] Quickbar { get; set; }

    [JsonIgnore]
    public ChatFlags ChatFlags { get; set; } = ChatFlags.All;

    public string Location { get; set; } = "";
    public string FavoriteFood { get; set; } = "";
    public string FavoriteMovie { get; set; } = "";
    public string FavoriteMusic { get; set; } = "";
    public string FavoritePerson { get; set; } = "";
    public string Hobbies { get; set; } = "";
    public string Introduction { get; set; } = "";

    public byte[] ProductionFlags { get; set; }

    public List<int> OwnedFarms { get; set; }
    public Farm Farm { get; set; }

    [JsonIgnore]
    public int TutorialState = 0;

    public PlayerData() { }
    public PlayerData(string name, byte gender, byte bloodType, byte birthMonth, byte birthDay, int[] entities) {
        Name = name;
        Gender = gender;
        BloodType = bloodType;
        BirthMonth = birthMonth;
        BirthDay = birthDay;
        BaseEntities = entities;
        DisplayEntities = (int[])entities.Clone();

        Inventory = new InventoryItem[50];
        Equipment = new InventoryItem[14];
        QuestFlags = new Dictionary<int, QuestStatus>();
        Quickbar = new int[10];
        Levels = new short[9];
        Exp = new int[9];
        Friendship = new short[7];
        ProductionFlags = new byte[576];

        for(int i = 0; i < 9; i++) {
            Levels[i] = 1;
        }
    }

    internal void Init(Client client) {
        // Dynamically load Display Entities

        // todo: remove down the line
        ProductionFlags ??= new byte[576];
        CheckpointFlags ??= new Dictionary<int, int>();
        Npcs ??= new HashSet<int>();
        Keys ??= new HashSet<int>();
        Dreams ??= new HashSet<int>();
        Farm ??= new Farm();
        Farm.Init(client);
        Tools ??= new InventoryItem[3];
        QuestFlags1 ??= new Dictionary<int, uint>();
        if(OwnedFarms == null) {
            OwnedFarms = new List<int>();
            OwnedFarms.Add(1); // base farm
        }

        DisplayEntities = new int[18];
        UpdateEntities();
        UpdateStats();
        Hp = MaxHp;
        Sta = MaxSta;
    }

    public void WriteEntities(PacketBuilder b) {
        for(int i = 0; i < 18; i++) {
            b.WriteInt(DisplayEntities[i]);
        }
    }

    public void WriteLevels(PacketBuilder b) {
        b.WriteInt(Levels[0]); // overall level
        b.WriteInt(Exp[0]); // level progress

        b.WriteByte(0); // ???
        b.WriteByte(0); // ???
        b.WriteByte(0); // ???
        b.WriteByte(0); // unused?

        // skill levels
        for(int i = 1; i <= 8; i++) {
            b.WriteShort(Levels[i]);
        }

        // skill exp
        for(int i = 1; i <= 8; i++) {
            b.WriteInt(Exp[i]);
        }
    }

    public void WriteStats(PacketBuilder b) {
        b.WriteInt(Hp);
        b.WriteInt(MaxHp);
        b.WriteInt(Sta);
        b.WriteInt(MaxSta);
        b.WriteInt(Attack);
        b.WriteInt(Defense);
        b.WriteUShort(Crit);
        b.WriteUShort(Dodge);
        // TODO: byte[12] buffs , short[12] buffTimes
    }

    public byte GetConstellation() {
        // 1 : AQU // 2 : PIS // 3 : ARI // 4 : TAU
        // 5 : GEM // 6 : CAN // 7 : LEO // 8 : VIR
        // 9 : LIB // 10: SCO // 11: SAG // 12: CAP
        return (byte)(BirthMonth switch {
            01 => BirthDay < 20 ? 12 : 01,
            02 => BirthDay < 19 ? 01 : 02,
            03 => BirthDay < 21 ? 02 : 03,
            04 => BirthDay < 20 ? 03 : 04,
            05 => BirthDay < 21 ? 04 : 05,
            06 => BirthDay < 21 ? 05 : 06,
            07 => BirthDay < 23 ? 06 : 07,
            08 => BirthDay < 23 ? 07 : 08,
            09 => BirthDay < 23 ? 08 : 09,
            10 => BirthDay < 23 ? 09 : 10,
            11 => BirthDay < 22 ? 10 : 11,
            12 => BirthDay < 22 ? 11 : 12,
            _ => throw new Exception("out of range birthday")
        });
    }

    public int GetToolLevel(Skill type) {
        var ind = type switch {
            Skill.Gathering => 0,
            Skill.Mining => 1,
            Skill.Woodcutting => 2,
            // Skill.Farming => 3,
        };

        var item = Tools[ind];
        return item.Id == 0 ? 1 : item.Data.Level;
    }

    public void UpdateStats() {
        var hp = 100;
        var sta = 100;
        var attack = 10;
        var defense = 10;
        var crit = 0;
        var dodge = 0;

        foreach(var item in Equipment) {
            if(item.Id == 0)
                continue;

            var att = Program.items[item.Id];
            Debug.Assert(att.Type == ItemType.Equipment);

            var equ = Program.equipment[att.SubId];

            hp += equ.EnergyIncrease;
            sta += equ.ActionPoints;
            attack += equ.EnergyDrain;
            defense += equ.DefenseValue;
            crit += equ.CritValue;
            dodge += equ.DodgeValue;
        }

        MaxHp = hp;
        MaxSta = sta;
        Attack = attack;
        Defense = defense;
        Crit = (ushort)crit;
        Dodge = (ushort)dodge;

        if(Hp > MaxHp)
            Hp = MaxHp;

        if(Sta > MaxSta)
            Sta = MaxSta;
    }

    public void UpdateEntities() {
        BaseEntities.CopyTo(DisplayEntities, 0);

        foreach(var item in Equipment) {
            if(item.Id == 0)
                continue;

            var att = Program.items[item.Id];

            Debug.Assert(att.Type == ItemType.Equipment);
            var equ = Program.equipment[att.SubId];
            var slot = equ.GetEntSlot();

            DisplayEntities[slot] = item.Id;
        }

        // TODO: broadcast new appearance
    }

    public void ReturnFromFarm() {
        var map = (StandardMap)Program.maps[ReturnMap]; // potentially throws cast exception

        CurrentMap = map.Id;
        PositionX = map.MapData.FarmX;
        PositionY = map.MapData.FarmY;
    }
}
