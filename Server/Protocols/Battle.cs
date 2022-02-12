using System;
using System.IO;

namespace Server.Protocols {
    struct MobData : IWriteAble {
        public int Id { get; set; }
        public int AttId { get; set; }
        
        public int X { get; set; }
        public int Y { get; set; }
        public byte Direction { get; set; }
        public byte Speed => 10;
        
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Disabled { get; set; }
        public byte State { get; set; }

        public int MoveX { get; set; }
        public int MoveY { get; set; }

        
        public void Write(PacketBuilder b) {
            b.WriteInt(Id);
            b.WriteInt(X);
            b.WriteInt(Y);
            b.WriteInt(AttId);

            b.WriteShort(Speed);
            b.WriteByte(Direction);
            b.WriteByte(State);

            b.WriteInt(Hp);
            b.WriteInt(MaxHp);
            b.WriteInt(Disabled);
            b.WriteInt(MoveX);
            b.WriteInt(MoveY);
        }
    }

    class Battle {
        public static void Handle(Client client) {
            switch (client.ReadByte()) {
                case 3: // 00537da8
                case 7: // 00537e23
                case 8: // 00537e98
                case 9: // 00537f23
                    break;
                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        public static void Send0C_01(Stream res, MobData[] mobs) {
            // create npcs
            var b = new PacketBuilder();

            b.WriteByte(0x0C); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(mobs.Length); // count

            b.BeginCompress();
            foreach(var mob in mobs) {
                b.Write(mob);
            }
            b.EndCompress();

            b.Send(res);
        }
    }
}