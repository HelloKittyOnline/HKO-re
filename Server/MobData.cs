using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Extractor;
using Server.Protocols;

namespace Server;

class MobData : IWriteAble {
    public readonly int Id;
    public readonly int MobId;

    public readonly int SpawnX;
    public readonly int SpawnY;
    public readonly int MoveDelay;

    // dynamic data
    public int X { get; set; }
    public int Y { get; set; }
    public byte Direction { get; set; }
    public int Hp { get; set; }
    // public byte State { get; set; }

    public Client Target { get; set; } = null;

    public int IsPet => 0;
    public byte Speed => 50; // units per second?
    public int MaxHp => Data.Hp;

    public MobAtt Data => Program.mobAtts[MobId];

    public MobData(int id, int mobId, int x, int y) {
        Id = id;
        MobId = mobId;
        SpawnX = X = x;
        SpawnY = Y = y;
        Direction = 5;
        Hp = MaxHp;

        MoveDelay = Random.Shared.Next(0, 4000);
    }

    public void Write(ref PacketBuilder b) {
        b.WriteInt(Id);
        b.WriteInt(X);
        b.WriteInt(Y);
        b.WriteInt(MobId);

        b.WriteShort(Speed);
        b.WriteByte(Direction);
        b.WriteByte((byte)(Hp == 0 ? 4 : 1));

        b.WriteInt(Hp);
        b.WriteInt(MaxHp);
        b.WriteInt(IsPet);
        b.WriteInt(X); // moving?
        b.WriteInt(Y); // moving?
    }

    public async Task QueueRespawn(Instance map) {
        Battle.SendMobState(map.Players, this, 4);
        await Task.Delay(Data.RespawnTime * 1000);
        Hp = MaxHp;

        Battle.SendMobState(map.Players, this, 1);
    }

    public void AttackTarget(IEnumerable<Client> clients) {
        lock(Target.Lock) {
            var damage = Math.Max(Data.Attack - (Target.Player.Defense / 20) + (Data.Level - Target.Player.Levels[(int)Skill.General]) + 1, 1);
            if(Random.Shared.Next(10000) < Target.Player.Dodge) {
                damage = 0;
            }

            Target.Player.Hp -= damage;

            Battle.SendDamageToPlayer(clients, Target.Id, Id, (short)damage, 0, 0);
            Player.SendPlayerHpSta(Target);
            if(Target.Player.Hp <= 0) {
                Target.Player.Hp = 0;
                Player.SendTakeBreak(Target, true);

                Target = null;
                Hp = MaxHp;
                Battle.SendMobState(clients, this, 1);
            }
        }
    }
    public float DistanceToSpawn() {
        var dx = X - SpawnX;
        var dy = Y - SpawnY;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public void AbortFollow(IEnumerable<Client> clients) {
        Target = null;
        X = SpawnX + Random.Shared.Next(200);
        Y = SpawnY + Random.Shared.Next(200);
        Hp = MaxHp; // hp resets when loosing player
        Battle.SendMobState(clients, this, 3);
        Battle.SendMobMove(clients, this, Speed * 2);
    }

    public static void RandomMove(MobData mob, IEnumerable<Client> clients) {
        lock(mob) {
            if(mob.Target != null)
                return;
            // move randomly in wanderRange
            var dx = mob.X - mob.SpawnX;
            var dy = mob.Y - mob.SpawnY;
            var dist = (int)Math.Sqrt(dx * dx + dy * dy);

            var len = 90;
            if(dist > 200) {
                mob.X -= (int)(dx / (float)dist * len);
                mob.Y -= (int)(dy / (float)dist * len);
                // move toward home
            } else {
                var (Sin, Cos) = Math.SinCos(2 * Math.PI * Random.Shared.NextSingle());
                mob.X += (int)(Sin * len);
                mob.Y += (int)(Cos * len);
            }

            if(mob.X < 0)
                mob.X = 0;
            if(mob.Y < 0)
                mob.Y = 0;

            Battle.SendMobMove(clients, mob, mob.Speed);
        }
    }
}
