using System.Diagnostics;

namespace Server {
    class PlayerData {
        public int Id { get; set; } = 1;

        public int CurrentMap { get; set; } = 8; // Sanrio_Harbour
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

        public int Money { get; set; } = 0;

        public int Hp { get; set; } = 100;
        public int MaxHp { get; set; } = 100;
        public int Sta { get; set; } = 100;
        public int MaxSta { get; set; } = 100;

        public PlayerData() { }
        public PlayerData(string name, byte gender, byte bloodType, byte birthMonth, byte birthDay, int[] entities) {
            Name = name;
            Gender = gender;
            BloodType = bloodType;
            BirthMonth = birthMonth;
            BirthDay = birthDay;
            DisplayEntities = entities;

            Debug.Assert(entities.Length == 18);
        }
    }
}