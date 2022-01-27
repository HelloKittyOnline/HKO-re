using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading;

namespace Server {
    struct InventoryItem {
        public int Id { get; set; }
        public byte Count { get; set; }
        public byte Durability { get; set; }
    }

    class PlayerData {
        [JsonIgnore] public int Id { get; } = IdManager.GetId();

        public int CurrentMap { get; set; } = 8; // Sanrio Harbour
        public int PositionX { get; set; } = 7730;
        public int PositionY { get; set; } = 6040;

        public string Name { get; set; }
        public byte Gender { get; set; }
        public byte BloodType { get; set; }
        public byte BirthMonth { get; set; }
        public byte BirthDay { get; set; }

        // [0] = body 
        // [1] = ? 
        // [2] = eyes
        // [3] = shoes
        // [4] = pants
        // [5] = clothes
        // [6] = hair
        // ...
        // [17] = ?
        public int[] DisplayEntities { get; set; }

        public int Money { get; set; }
        /*public int Tokens { get; set; }
        public int Money { get; set; }
        public int Tickets { get; set; }*/

        public int Hp { get; set; } = 100;
        public int MaxHp { get; set; } = 100;
        public int Sta { get; set; } = 100;
        public int MaxSta { get; set; } = 100;

        public const int Level = 1; // TODO

        [JsonIgnore] public int InventorySize => Math.Min(50, 25 + Level / 25);
        public InventoryItem[] Inventory { get; set; }

        // used for harvest canceling
        internal CancellationTokenSource cancelSource;

        public PlayerData() { }
        public PlayerData(string name, byte gender, byte bloodType, byte birthMonth, byte birthDay, int[] entities) {
            Name = name;
            Gender = gender;
            BloodType = bloodType;
            BirthMonth = birthMonth;
            BirthDay = birthDay;
            DisplayEntities = entities;

            Inventory = new InventoryItem[50];

            Debug.Assert(entities.Length == 18);
        }
        ~PlayerData() {
            IdManager.FreeId(Id);
        }

        public int AddItem(int item) {
            int open = -1;

            for(int i = 0; i < InventorySize; i++) {
                var asd = Inventory[i];
                if(asd.Id == 0 && open == -1) {
                    open = i;
                }
                if(asd.Id == item && asd.Count < 99) {
                    Inventory[i].Count++;
                    return i;
                }
            }

            if(open != -1) {
                Inventory[open].Id = item;
                Inventory[open].Count++;
            }
            return open;
        }
    }
}