using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Extractor;
using Server.Protocols;

namespace Server;

enum QuestStatus {
    None = 0,
    Running = 1,
    Done = 2
}

class DreamCarnivalData {
    public InventoryItem[] invCache { get; set; } = null;
    public InventoryItem[] equCache { get; set; } = null;
    public InventoryItem[] toolCache { get; set; } = null;

    public BitVector gaurdianDialog { get; set; } = new BitVector(16);

    public short guardianPet { get; set; } = 0;

    // used for some tutorial dialog
    [JsonIgnore] public int TutorialState = 0;

    public DreamCarnivalData() { }
}

class PlayerData {
    public int CurrentMap { get; set; } = 15; // Dream Room 1
    public int RespawnMap { get; set; } = 15; // map to return to when entering special maps for example farms

    [JsonIgnore] public Instance Map => Program.maps.GetValueOrDefault(CurrentMap);

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

    public int PositionX { get; set; } = 7450;
    public int PositionY { get; set; } = 5420;

    [JsonIgnore] public int TargetX;
    [JsonIgnore] public int TargetY;
    [JsonIgnore] public byte Rotation { get; set; } = 4;
    [JsonIgnore] public byte Speed => 200;

    // animation state
    // 1 = standing
    // 3 = sitting
    // 4 = gathering
    [JsonIgnore] public byte State { get; set; }

    // status icon
    [JsonIgnore] public int Status { get; set; }

    public string Name { get; set; }
    public byte Gender { get; set; }
    public byte BloodType { get; set; }
    public byte BirthMonth { get; set; }
    public byte BirthDay { get; set; }

    public int[] BaseEntities { get; set; }

    public int Money { get; set; }
    public int NormalTokens { get; set; }
    public int SpecialTokens { get; set; }
    public int Tickets { get; set; }

    public int Hp { get; set; } = 100;
    public int Sta { get; set; } = 20;
    [JsonIgnore] public int MaxHp { get; set; }
    [JsonIgnore] public int MaxSta { get; set; }
    [JsonIgnore] public int Attack { get; set; }
    [JsonIgnore] public int Defense { get; set; }
    [JsonIgnore] public ushort Crit { get; set; }
    [JsonIgnore] public ushort Dodge { get; set; }

    public short[] Levels { get; set; }
    public int[] Exp { get; set; }
    public short[] Friendship { get; set; }

    // TODO: split into completed and runing quests
    public Dictionary<int, QuestStatus> QuestFlags { get; set; } // todo: eventually rename to globalFlags
    public Dictionary<int, int> CheckpointFlags { get; set; }
    public Dictionary<int, uint> QuestFlags1 { get; set; }

    // used for encyclopedia
    public HashSet<int> Npcs { get; set; }
    public HashSet<int> Keys { get; set; }
    public HashSet<int> Dreams { get; set; }
    public HashSet<int> Cards { get; set; }
    public BitVector VisitedMaps { get; set; }

    [JsonIgnore] public int InventorySize => Math.Min(50, 24 + Levels[(int)Skill.General]);
    public InventoryItem[] Inventory { get; set; }
    public InventoryItem[] Equipment { get; set; }
    public InventoryItem[] Tools { get; set; }

    public int[] Quickbar { get; set; }

    [JsonIgnore] public ChatFlags ChatFlags { get; set; } = ChatFlags.All;

    public string Location { get; set; } = "";
    public string FavoriteFood { get; set; } = "";
    public string FavoriteMovie { get; set; } = "";
    public string FavoriteMusic { get; set; } = "";
    public string FavoritePerson { get; set; } = "";
    public string Hobbies { get; set; } = "";
    public string Introduction { get; set; } = "";

    public BitVector ProductionFlags { get; set; }

    public List<int> OwnedFarms { get; set; }
    public Farm Farm { get; set; }

    public PetData[] Pets { get; set; }
    public int ActivePet { get; set; } = -1;
    [JsonIgnore] public PetData Pet => ActivePet == -1 ? null : Pets[ActivePet];

    // 1 = combat
    // 2 = gathering
    [JsonIgnore] public int CurrentAction = 0;

    public DreamCarnivalData DreamCarnival { get; set; } = new();

    public PlayerData() { }
    public PlayerData(string name, byte gender, byte bloodType, byte birthMonth, byte birthDay, int[] entities) {
        Name = name;
        Gender = gender;
        BloodType = bloodType;
        BirthMonth = birthMonth;
        BirthDay = birthDay;
        BaseEntities = entities;
        Inventory = new InventoryItem[50];
        Equipment = new InventoryItem[14];
        QuestFlags = new Dictionary<int, QuestStatus>();
        Quickbar = new int[10];
        Levels = new short[9];
        Exp = new int[9];
        Friendship = new short[7];

        Levels.AsSpan().Fill(1);
    }

    internal void Init(Client client) {
        // todo: remove down the line
        ProductionFlags ??= new BitVector(576);
        CheckpointFlags ??= [];
        Npcs ??= [];
        // adjust indecies
        Keys = Keys?.Select(x => x > 7000 ? x - 7000 : x)?.ToHashSet() ?? [];
        Dreams ??= [];
        if(Cards == null) {
            Cards = [];
            // scan inventories for existing cards and update level (charges)
            for(int i = 0; i < Inventory.Length; i++) {
                ref var item = ref Inventory[i];
                if(item.Data.Type == ItemType.Card) {
                    item.Charges = 1;
                    Cards.Add(item.Data.SubId);
                }
            }
            if(Farm?.Inventory != null) {
                for(int i = 0; i < Farm.Inventory.Length; i++) {
                    ref var item = ref Farm.Inventory[i];
                    if(item.Data.Type == ItemType.Card) {
                        item.Charges = 1;
                        Cards.Add(item.Data.SubId);
                    }
                }
            }
        }
        Farm ??= new Farm();
        Farm.Init(client);
        Tools ??= new InventoryItem[3];
        QuestFlags1 ??= [];
        OwnedFarms ??= [1]; // base farm
        Pets ??= new PetData[3];
        for(int i = 0; i < 3; i++) {
            if(Pets[i]?.CardItemId == 0)
                Pets[i] = null;
            Pets[i]?.calcStats();
        }

        VisitedMaps ??= new BitVector(32);
        VisitedMaps[1] = true; // starting map: Dream Room 1

        TargetX = PositionX;
        TargetY = PositionY;
        // todo: ensure all fixed arrays are the right size in case something changes

        // remove dream room pants and dream ticket
        static void ClearInv(InventoryItem[] inv) {
            foreach(ref var item in inv.AsSpan()) {
                if(item.Id is 15200 or 10300) {
                    item = new InventoryItem();
                }
            }
        }
        ClearInv(client.Player.Inventory);
        ClearInv(client.Player.Equipment);
        ClearInv(client.Player.Tools);
        ClearInv(client.Player.Farm.Inventory);
        foreach(var item in client.Player.Pets) {
            if(item != null)
                ClearInv(item.Inventory);
        }

        UpdateStats();
    }

    public void WriteEntities(PacketBuilder b) {
        var entities = (int[])BaseEntities.Clone();

        foreach(var item in Equipment) {
            if(item.Id == 0)
                continue;

            var att = Program.items[item.Id];

            Debug.Assert(att.Type == ItemType.Equipment);
            var equ = Program.equipment[att.SubId];
            var slot = equ.GetEntSlot();

            entities[slot] = item.Id;
        }

        for(int i = 0; i < 18; i++) {
            b.WriteInt(entities[i]);
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
            _ => throw new ArgumentOutOfRangeException(nameof(type))
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

        if(ActivePet != -1) {
            var pet = Pets[ActivePet];
            // pet.calcStats();

            hp += pet.Hp;
            sta += pet.Sta;
            attack += pet.Atk;
            defense += pet.Def;
            crit += (ushort)pet.Crit;
            dodge += (ushort)pet.Dodge;
        }

        MaxHp = hp;
        MaxSta = sta;
        Attack = attack;
        Defense = defense;
        Crit = (ushort)Math.Min(10000, crit);
        Dodge = (ushort)Math.Min(10000, dodge); // capped at 100%

        if(Hp > MaxHp)
            Hp = MaxHp;

        if(Sta > MaxSta)
            Sta = MaxSta;
    }
}
