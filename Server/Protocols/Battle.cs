using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // TODO: improve damage formula
            while(true) {
                await Task.Delay(500);
                if(token.IsCancellationRequested)
                    break;

                lock(mob) {
                    if(mob.Hp == 0) // other player has killed mob
                        break;

                    var mobDamage = Math.Min(mob.Hp, 10);

                    mob.Hp -= mobDamage;
                    SendDamageToMob(map.Players, client.Id, mob.Id, (short)mobDamage, 0, 0);
                    if(mob.Hp <= 0) {
                        mob.Hp = 0;
                        mob.State = 4;
                        mob.QueueRespawn(map);

                        var mobAtt = Program.mobAtts[mob.MobId];
                        client.AddFromLootTable(mobAtt.LootTable);

                        break;
                    }
                }

                await Task.Delay(500);
                if(token.IsCancellationRequested)
                    break;

                lock(client.Player) {
                    var playerDamage = Math.Min(client.Player.Hp, 10);

                    // TODO: implement player hp and stamina
                    // client.Player.Hp -= playerDamage;
                    SendDamageToPlayer(map.Players, client.Id, mob.Id, (short)playerDamage, 0, 0);
                    if(client.Player.Hp <= 0) {
                        client.Player.Hp = 0;
                        break;
                    }
                }
            }
        }, () => {

        });
    }

    [Request(0x0C, 0x07)] // 00537e23
    private static void TakeBreak(ref Req req, Client client) {
        // after defeat message box ok
        throw new NotImplementedException();
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

    // 0C_02 - Mob Movement Packet
    // Client interpolates from current position to target position
    public static void SendMobMove(IEnumerable<Client> clients, MobData mob) {
        if (mob.Hp <= 0) return; // Dead mobs don't move

        var b = new PacketBuilder(0x0C, 0x02);

        b.WriteInt(mob.Id);              // Mob entity ID
        b.WriteShort((short)mob.TargetX); // Target X position
        b.WriteShort((short)mob.TargetY); // Target Y position
        b.WriteShort(mob.Speed);          // Movement speed

        b.WriteByte(0);                   // Unused
        b.WriteByte(0);                   // Sound flag (2 = play sound)

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
    public static void SendMobState(IEnumerable<Client> clients, MobData mob) {
        var b = new PacketBuilder(0x0C, 0x04);

        b.WriteInt(mob.Id);
        b.WriteByte(mob.State);

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

    #endregion
}
