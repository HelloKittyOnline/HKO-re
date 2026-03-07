using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extractor;

namespace Server.Protocols;

static class Battle {
    #region Request
    [Request(0x0C, 0x03)] // 00537da8
    private static void AttackMob(ref Req req, Client client) {
        var mobEntId = req.ReadInt32();

        var map = client.Player.Map;
        var mob = map.Mobs.FirstOrDefault(x => x.Id == mobEntId);
        if(mob == null)
            return;

        if(mob.Hp == 0)
            return;

        client.StartAction(async token => {
            var mobAtt = Program.mobAtts[mob.MobId];
            client.Player.CurrentAction = 1;

            while(true) {
                await Task.Delay(1000, token);

                lock(mob) {
                    if(mob.Hp == 0 || client.Player.Hp == 0)
                        break;

                    mob.Target ??= client;

                    var damage = Math.Max(client.Player.Attack - mobAtt.Defense / 20 + (client.Player.Levels[(int)Skill.General] - mobAtt.Level) + 1, 1);
                    if(Random.Shared.Next(10000) < client.Player.Crit) {
                        damage *= 2;
                    }

                    mob.Hp -= damage;
                    SendDamageToMob(map.Players, client.Id, mob.Id, (short)damage, 0, 0);
                    if(mob.Hp <= 0) {
                        mob.Hp = 0;
                        mob.Target = null;
                        _ = mob.QueueRespawn(map);

                        // if quest is done use secondary drop table
                        if(mobAtt.Quest != 0 && client.Player.QuestFlags.GetValueOrDefault(mobAtt.Quest, QuestStatus.None) == QuestStatus.Done) {
                            client.AddFromLootTable(mobAtt.LootTable2);
                        } else {
                            client.AddFromLootTable(mobAtt.LootTable1);
                        }
                        break;
                    }
                }
            }
            client.Player.CurrentAction = 0;
        }, () => {
            client.Player.CurrentAction = 0;
        });
    }

    [Request(0x0C, 0x07)] // 00537e23
    private static void TakeBreak(ref Req req, Client client) {
        // after defeat message box ok
        var playerId = req.ReadInt32();

        lock(client.Lock) {
            client.Player.Hp = client.Player.MaxHp / 10;
            client.Player.Sta = client.Player.MaxSta / 10;

            Player.LoadReturnMap(client);
            Player.ChangeMap(client, client.Player.CurrentMap, client.Player.PositionX, client.Player.PositionY);
        }

        Player.SendPlayerHpSta(client);
        Player.SendTakeBreak(client, false);
    }

    [Request(0x0C, 0x08)] // pet mob? 00537e98
    private static void Recieve08(ref Req req, Client client) {
        var petEntId = req.ReadInt32();
        throw new NotImplementedException();
    }

    [Request(0x0C, 0x09)] // feed mob? 00537f23
    private static void Recieve09(ref Req req, Client client) {
        var petEntId = req.ReadInt32();
        var invSlot = req.ReadByte();
        throw new NotImplementedException();
    }
    #endregion

    #region Response
    // 0C_01
    public static void SendMobs(Client client, IReadOnlyCollection<MobData> mobs) {
        var b = new PacketBuilder(0x0C, 0x01);

        b.WriteInt(mobs.Count); // count

        b.BeginCompress();
        foreach(var mob in mobs) {
            b.Write(mob);
        }
        b.EndCompress();

        b.Send(client);
    }

    // 0C_02
    public static void SendMobMove(IEnumerable<Client> clients, MobData mob, int speed) {
        // if (mob.Hp == 0) return;

        var b = new PacketBuilder(0x0C, 0x02);

        b.WriteInt(mob.Id); // count
        b.WriteShort((short)mob.X);
        b.WriteShort((short)mob.Y);
        b.WriteShort((short)speed);

        b.WriteByte(0); // unused
        b.WriteByte(0); // if == 2 play sound

        b.Send(clients);
    }

    // 0C_03
    public static void SendDamageToMob(IEnumerable<Client> clients, short playerId, int mobId, short d1, short d2, short d3) {
        var b = new PacketBuilder(0x0C, 0x03);

        b.WriteShort(playerId); // source player id
        b.WriteInt(mobId); // mob id

        b.WriteShort(d1); // damage 1 displayed in 250ms
        b.WriteShort(d2); // damage 2 displayed in 550ms
        b.WriteShort(d3); // damage 3 displayed in 850ms

        b.Send(clients);
    }

    // 0C_04
    public static void SendMobState(IEnumerable<Client> clients, MobData mob, byte state) {
        var b = new PacketBuilder(0x0C, 0x04);

        b.WriteInt(mob.Id);

        // 1 = normal
        // 2 = alert
        // 3 = squigly
        // 4 = sleeping
        // 5 = gone?
        // 6 = squigly also gone
        // 7 = normal
        b.WriteByte(state);

        b.Send(clients);
    }

    // 0C_05
    public static void SendPinataMessage(IEnumerable<Client> clients, byte type) {
        var b = new PacketBuilder(0x0C, 0x05);

        b.WriteByte(type);

        b.Send(clients);
    }

    // 0C_06
    public static void SendDamageToPlayer(IEnumerable<Client> clients, short playerId, int mobId, short d1, short d2, short d3) {
        // if(mob.Hp == 0) return;

        var b = new PacketBuilder(0x0C, 0x06);

        b.WriteInt(mobId); // mob id
        b.WriteShort(playerId); // player id

        b.WriteShort(d1); // damage 1 displayed in 250ms
        b.WriteShort(d2); // damage 2 displayed in 550ms
        b.WriteShort(d3); // damage 3 displayed in 850ms

        b.WriteShort(0); // unused ?
        b.WriteShort(0); // unused ?

        b.Send(clients);
    }

    // 0C_07
    // 0C_08
    // 0C_09
    // 0C_0A

    #endregion
}
