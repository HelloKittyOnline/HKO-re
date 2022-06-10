using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extractor;

namespace Server {
    class NpcData : NPCName {
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
        public int Dead { get; set; }
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
            Dead = 0;
            State = 0;
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
            b.WriteInt(Dead);
            b.WriteInt(X); // moving?
            b.WriteInt(Y); // moving?
        }

        public void QueueRespawn() {
            respawnTask ??= Task.Run(() => {
                Thread.Sleep(10 * 1000);
                Hp = MaxHp;
                Dead = 0;
                respawnTask = null;
            });
        }
    }

    class MapData {
        public int Id { get; set; }

        public Teleport[] Teleporters { get; set; }
        public Extractor.Resource[] Resources { get; set; }

        public NpcData[] Npcs { get; set; }
        public MobData[] Mobs { get; set; }
        
        public IEnumerable<Client> Players => Program.clients.Where(x => x.InGame && x.Player.CurrentMap == Id);
    }
}