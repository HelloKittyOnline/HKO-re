using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    class MobData : IWriteAble {
        public int Id { get; set; }

        // data
        public int MobId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public byte Direction { get; set; }
        public byte Speed => 10;
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Dead { get; set; }
        public byte State { get; set; }

        // TODO: ai state
        public bool InCombat { get; set; }
        public int CurrX => X;
        public int CurrY => Y;

        public void Write(PacketBuilder b) {
            b.WriteInt(Id);
            b.WriteInt(CurrX);
            b.WriteInt(CurrY);
            b.WriteInt(MobId);

            b.WriteShort(Speed);
            b.WriteByte(Direction);
            b.WriteByte(State);

            b.WriteInt(Hp);
            b.WriteInt(MaxHp);
            b.WriteInt(Dead);
            b.WriteInt(CurrX);
            b.WriteInt(CurrY);
        }
    }

    static class Battle {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 3: // 00537da8
                    AttackMob(client);
                    break;
                case 7: // 00537e23
                    TakeBreak(client);
                    break;
                case 8: // petting pet? 00537e98
                    Recieve08(client);
                    break;
                case 9: // feeding pet? 00537f23
                    Recieve09(client);
                    break;
                default:
                    client.Logger.LogWarning($"Unknown Packet 0C_{id:X2}");
                    break;
            }
        }

        #region Request
        // 0C_03
        private static void AttackMob(Client client) {
            var mobEntId = client.ReadInt32();
        }

        // 0C_07
        private static void TakeBreak(Client client) {
            // after defeat message box ok
        }

        // 0C_08
        private static void Recieve08(Client client) {
            var petEntId = client.ReadInt32();

        }
        // 0C_09
        private static void Recieve09(Client client) {
            var petEntId = client.ReadInt32();
            var invSlot = client.ReadByte();
        }
        #endregion

        #region Response
        // 0C_01
        public static void SendMobs(Client client, MobData[] mobs) {
            var b = new PacketBuilder();

            b.WriteByte(0x0C); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(mobs.Length); // count

            b.BeginCompress();
            foreach(var mob in mobs) {
                b.Write(mob);
            }
            b.EndCompress();

            b.Send(client);
        }

        // 0C_02
        public static PacketBuilder BuildMobMove(MobData mob) {
            // if (mob.Hp == 0) return;

            var b = new PacketBuilder();

            b.WriteByte(0x0C); // first switch
            b.WriteByte(0x02); // second switch

            b.WriteInt(mob.Id); // count
            b.WriteShort((short)mob.CurrX);
            b.WriteShort((short)mob.CurrY);
            b.WriteShort(mob.Speed);

            b.WriteByte(0); // unused
            b.WriteByte(0); // if == 2 play sound

            // b.Send(client);

            return b;
        }

        // 0C_02
        public static void DoDamage(Client client) {
            // if(mob.Hp == 0) return;

            var b = new PacketBuilder();

            b.WriteByte(0x0C); // first switch
            b.WriteByte(0x02); // second switch

            b.WriteShort(0); // source player id
            b.WriteInt(0); // mob id

            b.WriteShort(0); // damage 1 displayed in 250ms
            b.WriteShort(0); // damage 2 displayed in 550ms
            b.WriteShort(0); // damage 3 displayed in 850ms

            b.Send(client);
        }

        #endregion
    }
}