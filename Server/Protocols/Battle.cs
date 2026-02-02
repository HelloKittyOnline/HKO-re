using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extractor;

namespace Server.Protocols;

static class Battle {
    private static readonly Random _random = new();

    // Stamina cost per attack
    private const int StaminaCostPerAttack = 5;

    // Stamina recovery rate after combat (per second)
    private const int StaminaRecoveryRate = 10;
    private const int StaminaRecoveryInterval = 1000; // ms

    #region Combat Calculations

    /// <summary>
    /// Calculate damage dealt from attacker to defender.
    /// Formula: max(1, Attack - Defense/2) with variance ±20%
    /// </summary>
    private static int CalculateDamage(int attack, int defense) {
        var baseDamage = attack - (defense / 2);
        baseDamage = Math.Max(1, baseDamage);

        // Add ±20% variance
        var variance = (int)(baseDamage * 0.2);
        var finalDamage = baseDamage + _random.Next(-variance, variance + 1);

        return Math.Max(1, finalDamage);
    }

    /// <summary>
    /// Check if attack is a critical hit.
    /// Crit chance = Crit stat / 10 (capped at 50%)
    /// </summary>
    private static bool IsCriticalHit(int critStat) {
        var critChance = Math.Min(50, critStat / 10);
        return _random.Next(100) < critChance;
    }

    /// <summary>
    /// Check if attack is dodged.
    /// Dodge chance = Dodge stat / 10 (capped at 50%)
    /// </summary>
    private static bool IsDodged(int dodgeStat) {
        var dodgeChance = Math.Min(50, dodgeStat / 10);
        return _random.Next(100) < dodgeChance;
    }

    /// <summary>
    /// Calculate player damage to mob with crit and mob dodge.
    /// Returns (damage, isCrit, isDodged).
    /// </summary>
    private static (int damage, bool isCrit, bool isDodged) CalculatePlayerToMobDamage(PlayerData player, MobAtt mobAtt) {
        // Check if mob dodges (using Dodge field, formerly Unknown7)
        if (mobAtt.Dodge > 0 && IsDodged(mobAtt.Dodge)) {
            return (0, false, true); // Mob dodged!
        }

        var damage = CalculateDamage(player.Attack, mobAtt.Defense);
        var isCrit = IsCriticalHit(player.Crit);

        if (isCrit) {
            damage *= 2; // Critical hits deal double damage
        }

        return (damage, isCrit, false);
    }

    /// <summary>
    /// Calculate mob damage to player with player dodge.
    /// Returns damage. Damage of 0 means dodged.
    /// </summary>
    private static int CalculateMobToPlayerDamage(MobAtt mobAtt, PlayerData player) {
        // Check if player dodges
        if (IsDodged(player.Dodge)) {
            return 0; // Dodged!
        }

        return CalculateDamage(mobAtt.Attack, player.Defense);
    }

    /// <summary>
    /// Handle player defeat - restore HP/Sta, teleport to last checkpoint or safe location.
    /// </summary>
    private static void HandlePlayerDefeat(Client client) {
        var player = client.Player;

        // Restore HP and Stamina to full
        player.Hp = player.MaxHp;
        player.Sta = player.MaxSta;

        // Find the player's last checkpoint or default spawn location
        int targetMap = 8; // Default: London starting area
        int targetX = 352;
        int targetY = 688;

        // Check player's checkpoint flags for last activated checkpoint
        if (player.CheckpointFlags != null && player.CheckpointFlags.Count > 0) {
            // Find the highest checkpoint ID the player has activated (status >= 1)
            var lastCheckpoint = player.CheckpointFlags
                .Where(x => x.Value >= 1)
                .Select(x => x.Key)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            if (lastCheckpoint > 0 && lastCheckpoint < Program.checkpoints.Length) {
                var checkpoint = Program.checkpoints[lastCheckpoint];
                if (checkpoint.Map > 0) {
                    targetMap = checkpoint.Map;
                    targetX = checkpoint.X;
                    targetY = checkpoint.Y;
                }
            }
        }

        // Update player position
        player.CurrentMap = targetMap;
        player.PositionX = targetX;
        player.PositionY = targetY;

        // Send defeat notification to client (0C_05)
        SendDefeatNotification(client);

        // Update player stats and trigger map change
        Player.SendPlayerHpSta(client);
    }

    /// <summary>
    /// Start stamina recovery for a player after combat ends.
    /// </summary>
    private static async void StartStaminaRecovery(Client client) {
        // Recover stamina over time until full
        while (client.InGame && client.Player.Sta < client.Player.MaxSta) {
            await Task.Delay(StaminaRecoveryInterval);

            if (!client.InGame) break;

            lock (client.Player) {
                if (client.Player.Sta < client.Player.MaxSta) {
                    client.Player.Sta = Math.Min(client.Player.MaxSta, client.Player.Sta + StaminaRecoveryRate);
                    Player.SendPlayerHpSta(client);
                }
            }
        }
    }

    #endregion

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

        // Check if player has stamina to attack
        if(client.Player.Sta < StaminaCostPerAttack) {
            Player.SendMessage(client, Player.MessageType.You_do_not_have_enough_Action_Points_1);
            return;
        }

        bool playerDefeated = false;

        client.StartAction(async token => {
            var mobAtt = Program.mobAtts[mob.MobId];

            while(true) {
                await Task.Delay(500);
                if(token.IsCancellationRequested)
                    break;

                // Player attacks mob
                lock(mob) {
                    if(mob.Hp == 0) // other player has killed mob
                        break;

                    // Consume stamina for attack
                    lock(client.Player) {
                        if(client.Player.Sta < StaminaCostPerAttack) {
                            // Out of stamina, stop attacking
                            Player.SendMessage(client, Player.MessageType.You_do_not_have_enough_Action_Points_1);
                            break;
                        }
                        client.Player.Sta -= StaminaCostPerAttack;
                        Player.SendPlayerHpSta(client);
                    }

                    // Calculate damage with crit and dodge
                    var (damage, isCrit, isDodged) = CalculatePlayerToMobDamage(client.Player, mobAtt);

                    if (isDodged) {
                        // Mob dodged - send 0 damage
                        SendDamageToMob(map.Players, client.Id, mob.Id, 0, 0, 0);
                    } else {
                        var actualDamage = Math.Min(mob.Hp, damage);
                        mob.Hp -= actualDamage;

                        // Send damage (use d2 for crit indicator)
                        if (isCrit) {
                            SendDamageToMob(map.Players, client.Id, mob.Id, (short)actualDamage, (short)actualDamage, 0);
                        } else {
                            SendDamageToMob(map.Players, client.Id, mob.Id, (short)actualDamage, 0, 0);
                        }

                        if(mob.Hp <= 0) {
                            mob.Hp = 0;
                            mob.State = 4;
                            mob.QueueRespawn(map);
                            client.AddFromLootTable(mobAtt.LootTable);
                            break;
                        }
                    }
                }

                await Task.Delay(500);
                if(token.IsCancellationRequested)
                    break;

                // Mob attacks player
                lock(client.Player) {
                    var playerDamage = CalculateMobToPlayerDamage(mobAtt, client.Player);

                    if (playerDamage == 0) {
                        // Player dodged
                        SendDamageToPlayer(map.Players, client.Id, mob.Id, 0, 0, 0);
                    } else {
                        var actualDamage = Math.Min(client.Player.Hp, playerDamage);
                        client.Player.Hp -= actualDamage;
                        SendDamageToPlayer(map.Players, client.Id, mob.Id, (short)actualDamage, 0, 0);
                        Player.SendPlayerHpSta(client);

                        if(client.Player.Hp <= 0) {
                            client.Player.Hp = 0;
                            playerDefeated = true;
                            break;
                        }
                    }
                }
            }
        }, () => {
            // Combat ended callback
            if (playerDefeated) {
                HandlePlayerDefeat(client);
            } else {
                // Start stamina recovery after combat
                StartStaminaRecovery(client);
            }
        });
    }

    /// <summary>
    /// Called when an aggressive mob attacks a player (from mob aggro system).
    /// </summary>
    public static void MobAttacksPlayer(MobData mob, Client target) {
        if (target == null || !target.InGame || target.Player.Hp <= 0)
            return;

        var mobAtt = Program.mobAtts[mob.MobId];
        var map = target.Player.Map;

        lock (target.Player) {
            var playerDamage = CalculateMobToPlayerDamage(mobAtt, target.Player);

            if (playerDamage == 0) {
                // Player dodged
                SendDamageToPlayer(map.Players, target.Id, mob.Id, 0, 0, 0);
            } else {
                var actualDamage = Math.Min(target.Player.Hp, playerDamage);
                target.Player.Hp -= actualDamage;
                SendDamageToPlayer(map.Players, target.Id, mob.Id, (short)actualDamage, 0, 0);
                Player.SendPlayerHpSta(target);

                if (target.Player.Hp <= 0) {
                    target.Player.Hp = 0;
                    mob.AggroTarget = null; // Clear aggro on defeated player
                    HandlePlayerDefeat(target);
                }
            }
        }
    }

    [Request(0x0C, 0x07)] // 00537e23
    private static void TakeBreak(ref Req req, Client client) {
        // Client clicked OK on defeat message box
        // Teleport player to their checkpoint location
        Player.SendChangeMap(client);
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

    // 0C_05 - Defeat Notification
    // Shows defeat dialog to player (type 1 = battle defeat, type 2 = other?)
    public static void SendDefeatNotification(Client client, byte defeatType = 1) {
        var b = new PacketBuilder(0x0C, 0x05);

        b.WriteByte(defeatType); // 1 = normal defeat, 2 = timeout defeat?

        b.Send(client);
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
