using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Extractor;
using Server.Protocols;

namespace Server {
    struct InventoryItem : IWriteAble {
        public static readonly InventoryItem Empty = new InventoryItem();

        public int Id { get; set; }
        public byte Count { get; set; }
        public byte Durability { get; set; }

        public void Write(PacketBuilder w) {
            w.WriteInt(Id); // id

            w.WriteByte(Count); // count
            w.WriteByte(Durability); // durability
            w.WriteByte(0); // pet something
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

    class PlayerData {
        public int CurrentMap { get; set; } = 1; // Dream Room 1
        [JsonIgnore]
        public MapData Map {
            get {
                if(CurrentMap < 30000) {
                    return Program.maps[CurrentMap];
                }

                if (CurrentMap % 10 == 7) {
                    return Program.maps[4]; // wtf
                }
                
                return Program.maps[CurrentMap % 10];
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

        [JsonIgnore] public int Hp => 100;
        [JsonIgnore] public int MaxHp => 100;
        [JsonIgnore] public int Sta => 100;
        [JsonIgnore] public int MaxSta => 100;

        public short[] Levels { get; set; }
        public int[] Exp { get; set; }
        public short[] Friendship { get; set; }

        public Dictionary<int, int> QuestFlags { get; set; }

        [JsonIgnore]
        public int InventorySize => Math.Min(50, 24 + Levels[(int)Skill.General]);
        public InventoryItem[] Inventory { get; set; }
        public InventoryItem[] Equipment { get; set; }

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
            QuestFlags = new Dictionary<int, int>();
            Quickbar = new int[10];
            Levels = new short[9];
            Exp = new int[9];
            Friendship = new short[7];
            ProductionFlags = new byte[576];

            for(int i = 0; i < 9; i++) {
                Levels[i] = 1;
            }

            Init();

            Debug.Assert(entities.Length == 18);
        }

        internal void Init() {
            // Dynamically load Display Entities

            // todo: remove down the line
            ProductionFlags ??= new byte[576];

            DisplayEntities = new int[18];
            BaseEntities.CopyTo(DisplayEntities, 0);
            foreach(var item in Equipment) {
                if(item.Id == 0)
                    continue;

                var att = Program.items[item.Id];
                var equ = Program.equipment[att.SubId];
                var slot = equ.GetEntSlot();

                DisplayEntities[slot] = item.Id;
            }
        }

        public void AddExpAction(Client client, Skill skill, int level) {
            AddExp(client, skill, (int)(600 / Math.Pow(2, Levels[(int)skill] - level + 1)));
        }

        public void AddExp(Client client, Skill skill, int gain) {
            var level = Levels[(int)skill];

            if(skill != Skill.General) {
                AddExp(client, Skill.General, gain / 2);
            }

            var required = Program.skills[level].GetExp(skill);
            Exp[(int)skill] += gain;

            if(required != 0 && Exp[(int)skill] >= required) {
                Levels[(int)skill]++;
                Exp[(int)skill] -= required;

                if(skill == Skill.General) {
                    Protocols.Inventory.SendSetInventorySize(client);
                }
            }
            Player.SendSkillChange(client, skill, true);
        }

        [Obsolete("Use client.AddItem instead.")]
        public int AddItem(int itemId, int count) {
            int open = -1;

            var itemData = Program.items[itemId];

            // add to existing stack or create new stack
            // todo: handle item overflow
            for(int i = 0; i < InventorySize; i++) {
                var invItem = Inventory[i];
                if(invItem.Id == 0 && open == -1) {
                    open = i;
                }
                if(invItem.Id == itemId && invItem.Count < itemData.StackLimit) {
                    Inventory[i].Count += (byte)count;
                    return i;
                }
            }

            if(open != -1) {
                Inventory[open].Id = itemId;
                Inventory[open].Count += (byte)count;
            }
            return open;
        }

        public bool HasItem(int itemId, int count = 1) {
            return GetItemCount(itemId) >= count;
        }
        public int GetItemCount(int itemId) {
            int count = 0;
            for(int i = 0; i < InventorySize; i++) {
                if(Inventory[i].Id == itemId) {
                    count += Inventory[i].Count;
                }
            }
            return count;
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
                b.WriteShort((short)Levels[i]);
            }

            // skill exp
            for(int i = 1; i <= 8; i++) {
                b.WriteInt(Exp[i]);
            }
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
    }
}