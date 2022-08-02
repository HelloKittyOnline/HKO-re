using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Server.Protocols;

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
            case 8: // pet mob? 00537e98
                Recieve08(client);
                break;
            case 9: // feed mob? 00537f23
                Recieve09(client);
                break;
            default:
                client.LogUnknown(0x0C, id);
                break;
        }
    }

    #region Request
    // 0C_03
    private static void AttackMob(Client client) {
        var mobEntId = client.ReadInt32();

        var map = client.Player.Map;
        var mob = map.Mobs.FirstOrDefault(x => x.Id == mobEntId);
        if(mob == null)
            return;

        var mobAtt = Program.mobAtts[mob.MobId];

        if(mob.Hp == 0)
            return;

        client.CancelAction();

        client.StartAction(token => {
            // TODO: improve damage formula
            while(true) {
                Thread.Sleep(500);
                if(token.IsCancellationRequested)
                    break;

                var mobDamage = Math.Min(mob.Hp, 10);

                mob.Hp -= mobDamage;
                BroadcastDamageToMob(map.Players, client.Id, mob.Id, (short)mobDamage, 0, 0);
                if(mob.Hp <= 0) {
                    mob.Hp = 0;
                    mob.State = 4;
                    mob.QueueRespawn();

                    var item = Program.lootTables[mobAtt.LootTable].GetRandom();
                    if(item != -1) {
                        client.AddItem(item, 1);
                    }

                    break;
                }

                Thread.Sleep(500);

                var playerDamage = Math.Min(client.Player.Hp, 10);

                // TODO: implement player hp and stamina
                // client.Player.Hp -= playerDamage;
                BroadcastDamageToPlayer(map.Players, client.Id, mob.Id, (short)playerDamage, 0, 0);
                if(client.Player.Hp <= 0) {
                    client.Player.Hp = 0;
                    break;
                }
            }
        }, () => {

        });
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
        b.WriteShort((short)mob.X);
        b.WriteShort((short)mob.Y);
        b.WriteShort(mob.Speed);

        b.WriteByte(0); // unused
        b.WriteByte(0); // if == 2 play sound

        // b.Send(client);

        return b;
    }

    // 0C_03
    public static void BroadcastDamageToMob(IEnumerable<Client> clients, short playerId, int mobId, short d1, short d2, short d3) {
        var b = new PacketBuilder();

        b.WriteByte(0x0C); // first switch
        b.WriteByte(0x03); // second switch

        b.WriteShort(playerId); // source player id
        b.WriteInt(mobId); // mob id

        b.WriteShort(d1); // damage 1 displayed in 250ms
        b.WriteShort(d2); // damage 2 displayed in 550ms
        b.WriteShort(d3); // damage 3 displayed in 850ms

        foreach(var client1 in clients) {
            b.Send(client1);
        }
    }

    // 0C_06
    public static void BroadcastDamageToPlayer(IEnumerable<Client> clients, short playerId, int mobId, short d1, short d2, short d3) {
        // if(mob.Hp == 0) return;

        var b = new PacketBuilder();

        b.WriteByte(0x0C); // first switch
        b.WriteByte(0x06); // second switch

        b.WriteInt(mobId); // mob id
        b.WriteShort(playerId); // player id

        b.WriteShort(d1); // damage 1 displayed in 250ms
        b.WriteShort(d2); // damage 2 displayed in 550ms
        b.WriteShort(d3); // damage 3 displayed in 850ms

        b.WriteShort(0); // unused ?
        b.WriteShort(0); // unused ?

        foreach(var client1 in clients) {
            b.Send(client1);
        }
    }

    #endregion
}
