using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extractor;

namespace Server.Protocols;

struct PetEntData : IWriteAble {
    public short OwnerId;
    public short PetId;
    public int X, Y;
    public string Name;

    public void Write(ref PacketBuilder b) {
        b.WriteShort(OwnerId);
        b.WriteShort(PetId);
        b.WriteInt(X);
        b.WriteInt(Y);
        b.WritePadWString(Name, 40);
    }
}

static class Pet {
    public const int pettingCooldown = 60; // 004f323d
    public const int pettingGain = 5;

    static Random feed_rng = new();
    public static void DoFeed(Client owner, Client actor, ItemRef item) {
        Debug.Assert(item.type == InvType.Player);
        Debug.Assert(item.Item.Data.Type == ItemType.Pet_Food);

        // todo: whate are the actual times?
        const int duration = 5 * 1000; // in ms
        const int cooldown = 120; // 004f3247

        var id = owner.Player.ActivePet;
        if(id == -1)
            return;

        var pet = owner.Player.Pets[id];
        var food = Program.petFood[item.Item.Data.SubId];

        // itemRef cannot be passed to lambda
        var itemId = item.Id;
        var itemSlot = item.Index;
        // todo: validate cooldowns

        void cancel() {
            SendFeedingProgress(actor, owner == actor ? 0 : 3);
        }

        actor.StartAction(async (token) => {
            SendFeedingProgress(actor, 1, duration);
            await Task.Delay(duration);

            if(token.IsCancellationRequested)
                return;

            // technically this is not 100% thread safe but making multiplayer interactions atomic is very hard
            if(pet != owner.Player.Pet) { // pet changed
                cancel();
                return;
            }

            // try to remove food from actors inventory
            lock(actor.Player) {
                var nItem = actor.GetItem(InvType.Player, itemSlot);
                if(nItem.Id == itemId && nItem.Count > 0) {
                    nItem.Remove(1);
                } else {
                    cancel();
                    return;
                }
            }

            lock(owner.Player) {
                pet.Hunger = Math.Clamp(pet.Hunger + food.Fullness, 0, 240);
                lock(feed_rng) {
                    if(feed_rng.Next(10000) < food.ExpChance) {
                        pet.AddExp(food.ExpAmount, owner);
                    }
                }

                // pet.NextFeedTime = DateTimeOffset.UtcNow.AddSeconds(cooldown);
                SendSetHunger(owner, id);
                SendShowEmoji(owner.Player.Map.Players, owner.Id, 4);
                if(actor == owner) {
                    SendSetEatingCooldown(owner, id, cooldown);
                    Task.Run(async () => {
                        for(int i = cooldown - 1; i >= 0; i--) {
                            await Task.Delay(1000);
                            if(!owner.InGame)
                                break;
                            SendSetEatingCooldown(owner, id, i);
                        }
                    });
                }

                // after 2 minutes becomes dirty
                if(pet.DirtyState == 0) {
                    // only start if pet is clean and no other cooldown is running
                    pet.DirtyState = 1;

                    Task.Delay(cooldown * 1000).ContinueWith((t) => {
                        lock(owner.Player) {
                            // not the same pet anymore
                            if(pet != owner.Player.Pets[id])
                                return;

                            pet.DirtyState = 2;
                            SendSetDirty(owner, id, true);
                            SendShowEmoji(owner.Player.Map.Players, owner.Id, 2);
                        }
                    });
                }
            }

            SendFeedingProgress(actor, 2);
        }, () => {
            cancel();
        });
    }

    static void DoClean(Client owner, Client actor) {
        const int duration = 5 * 1000; // in ms

        lock(owner.Player) {
            var pet = owner.Player.Pet;
            if(pet == null)
                return;
            if(pet.DirtyState != 2) {
                SendPetIsHygienic(actor);
                return;
            }

            actor.StartAction(async (token) => {
                SendCleaningProgress(actor, 1, duration);
                await Task.Delay(duration);

                if(token.IsCancellationRequested)
                    return;

                lock(owner.Player) {
                    if(pet != owner.Player.Pet)
                        return; // pet changed
                    if(pet.DirtyState != 2)
                        return; // pet got cleaned by someone else

                    // clean pet and place 5 Pet Jelly in inventory
                    pet.DirtyState = 0;
                    owner.AddItem(85, 5, true);
                    SendSetDirty(owner, owner.Player.ActivePet, false);
                }
                SendCleaningProgress(actor, 2);
                SendShowEmoji(owner.Player.Map.Players, owner.Id, 1);
            }, () => {
                SendCleaningProgress(actor, 0);
            });
        }
    }

    #region Request

    [Request(0x0D, 0x02)] // 00536928
    static void SetActivePet(ref Req req, Client client) {
        var id = req.ReadByte() - 1;

        if(id < 0 || id >= 3)
            return;

        lock(client.Player) {
            var pet = client.Player.Pets[id];
            if(pet == null)
                return;

            if(client.Player.ActivePet != -1) {
                // client should first send 0D_03 when changing but let's handle it anyway
                SendRemovePet(client.Player.Map.Players, client.Id);
            }

            client.Player.ActivePet = id;
            SendAddPetEnt(client.Player.Map.Players, pet.EntData(client));
            SendSetActivePet(client, id + 1);
            client.UpdateStats();
        }
    }

    [Request(0x0D, 0x03)] // 0053698a
    static void RemoveActivePet(ref Req req, Client client) {
        lock(client.Player) {
            if(client.Player.ActivePet == -1)
                return;
            client.Player.ActivePet = -1;
            SendRemovePet(client.Player.Map.Players, client.Id);
            SendSetActivePet(client, 0);
            client.UpdateStats();
        }
    }

    [Request(0x0D, 0x05)] // 00536a60
    static void SetPetName(ref Req req, Client client) {
        var id = req.ReadByte() - 1;
        var name = req.ReadWString();

        if(id < 0 || id >= 3)
            return;

        lock(client.Player) {
            var pet = client.Player.Pets[id];
            if(pet == null)
                return;

            pet.Name = name;
            SendPetData(client, id, pet);
            if(client.Player.ActivePet == id) {
                SendUpdatePetEnt(client.Player.Map.Players, pet.EntData(client));
            }
        }
    }

    [Request(0x0D, 0x06)] // 00536ae8
    static void RecvAnimationState(ref Req req, Client client) {
        var state = req.ReadByte(); // 7 or 9
        SendAnimationState(client.Player.Map.Players, client.Id, state);
    }

    [Request(0x0D, 0x07)] // 00536b83
    static void Move(ref Req req, Client client) {
        var x = req.ReadInt32();
        var y = req.ReadInt32();
        var speed = req.ReadInt32();

        SendMovePet(client.Player.Map.Players, client.Id, x, y);
    }

    [Request(0x0D, 0x09)] // 00536bea
    static void PetPet(ref Req req, Client client) {
        lock(client.Player) {
            if(client.Player.ActivePet == -1)
                return;

            // todo: validate player cooldown
            var pet = client.Player.Pets[client.Player.ActivePet];

            pet.Comfort = Math.Min(240, pet.Comfort + pettingGain);
            SendSetComfort(client, client.Player.ActivePet);
            SendShowEmoji(client.Player.Map.Players, client.Id, 1);

            SendSetPettingCooldown(client, pettingCooldown);
            Task.Delay(pettingCooldown * 1000).ContinueWith((t) => {
                // game does not clear cooldown by itself
                SendSetPettingCooldown(client, 0);
            });

        }
    }

    [Request(0x0D, 0x0A)] // 00536c6c
    static void CleanUp(ref Req req, Client client) {
        var a = req.ReadInt32(); // PetFecesToFertilizerItemID
        Debug.Assert(a == 85);

        DoClean(client, client);
    }

    [Request(0x0D, 0x0B)] // 00536cce
    static void BackToCard(ref Req req, Client client) {
        var id = client.Player.ActivePet;
        if(id == -1)
            return;

        lock(client.Player) {
            var pet = client.Player.Pets[id];

            if(client.AddItem(new InventoryItem { Id = pet.CardItemId, Count = 1, Charges = (byte)pet.Level }, false)) {
                client.Player.ActivePet = -1;
                client.Player.Pets[id] = null;

                SendRemovePet(client.Player.Map.Players, client.Id);
                SendPetData(client, id, null);
                SendSetActivePet(client, 0);
            }
        }
    }

    [Request(0x0D, 0x0C)] // 00536d53
    static void BreedPets(ref Req req, Client client) {
        var itemId = req.ReadInt32();
        var npcId = req.ReadInt32();
        Debug.Assert(npcId is 163 or 180);

        var itemInfo = Program.items[itemId];
        if(itemInfo.Type != ItemType.Card)
            return;

        var petInfo = Program.petInitData[itemInfo.SubId];
        if(petInfo.parent1_id == 0 || petInfo.parent2_id == 0)
            return;

        lock(client.Player) {
            if(client.Player.Levels[(int)Skill.General] < 20) {
                SendBreedResult(client, 1, itemInfo.SubId, 20, 0); // player level too low
                return;
            }
            if(client.Player.Money < petInfo.breed_price) {
                SendBreedResult(client, 4, itemInfo.SubId, petInfo.breed_price, 0); // not enough money
                return;
            }

            bool has1 = false;
            bool has1_lvl = false;

            bool has2 = false;
            bool has2_lvl = false;

            // check required parents
            foreach(var item in client.Player.Inventory) {
                var dat = item.Data;
                if(dat.Type == ItemType.Card) {
                    has1 |= dat.SubId == petInfo.parent1_id;
                    has1_lvl |= dat.SubId == petInfo.parent1_id && item.Charges >= petInfo.parent1_lvl;
                    has2 |= dat.SubId == petInfo.parent2_id;
                    has2_lvl |= dat.SubId == petInfo.parent2_id && item.Charges >= petInfo.parent2_lvl;
                }
            }

            if(!has1_lvl || !has2_lvl) {
                if(!has1) { // missing parent
                    SendBreedResult(client, 2, itemInfo.SubId, petInfo.parent1_id, 0);
                } else if(!has1_lvl) { // parent level too low
                    SendBreedResult(client, 3, itemInfo.SubId, petInfo.parent1_id, petInfo.parent1_lvl);
                }
                if(!has2) { // missing parent
                    SendBreedResult(client, 2, itemInfo.SubId, petInfo.parent2_id, 0);
                } else if(!has2_lvl) { // parent level too low
                    SendBreedResult(client, 3, itemInfo.SubId, petInfo.parent2_id, petInfo.parent2_lvl);
                }
                return;
            }

            if(client.AddItem(new InventoryItem { Id = itemId, Charges = 1, Count = 1 }, true)) {
                client.Player.Money -= petInfo.breed_price;
                Inventory.SendSetMoney(client);
                SendBreedResult(client, 0, itemInfo.SubId, 0, 0); // sucess
            }
        }
    }

    [Request(0x0D, 0x0D)] // 00536dc8
    static void ChangeState(ref Req req, Client client) {
        var state = req.ReadInt32();

        // maybe broadcast?
        // 1 = petting
        // 2 = dirty
        // 3 = ?
        // 6 = ?
        // 7 = when comfort == 0
    }

    [Request(0x0D, 0x0E)] // 00536e6e
    static void Recv0E(ref Req req, Client client) { // might be related to matching pets
        // timer field5_0x14 ran out?
        var a = req.ReadInt32();
        Debugger.Break();
        a = Math.Abs(a); // why????

        for(int i = 0; i < a; i++) {
            req.ReadInt32();
        }
    }

    [Request(0x0D, 0x0F)] // 00536ee8
    static void PetOtherPet(ref Req req, Client client) {
        var ownerId = req.ReadInt32();

        var owner = Program.clients.FirstOrDefault(x => x.Id == ownerId);
        if(owner != null) {
            lock(owner.Player) {
                if(owner.Player.ActivePet == -1)
                    return;

                // todo: validate player cooldown
                var pet = owner.Player.Pets[owner.Player.ActivePet];
                pet.Comfort = Math.Min(240, pet.Comfort + pettingGain);

                SendSetComfort(owner, owner.Player.ActivePet);
                SendShowEmoji(owner.Player.Map.Players, owner.Id, 1);
            }
        }
    }

    [Request(0x0D, 0x10)] // 00536f73
    static void FeedOtherPet(ref Req req, Client client) {
        // feed other pet food?
        var ownerId = req.ReadInt32();
        var slot = req.ReadByte() - 1;

        var item = client.GetItem(InvType.Player, slot);
        if(item.Item.Data.Type != ItemType.Pet_Food)
            return;

        var owner = Program.clients.FirstOrDefault(x => x.Id == ownerId);
        if(owner != null) {
            DoFeed(owner, client, item);
        }
    }

    [Request(0x0D, 0x11)] // 00536fe8
    static void OpenOtherPetInfo(ref Req req, Client client) {
        var ownerId = req.ReadInt32();

        var other = Program.clients.FirstOrDefault(x => x.Id == ownerId);
        if(other != null && other.Player.ActivePet != -1) {
            SendOtherPetInfo(client, other);
        }
    }

    [Request(0x0D, 0x12)] // 0053705c
    static void CleanOtherPet(ref Req req, Client client) {
        // Clean other pet
        var ownerId = req.ReadInt32();

        var owner = Program.clients.FirstOrDefault(x => x.Id == ownerId);
        if(owner != null) {
            DoClean(owner, client);
        }
    }

    [Request(0x0D, 0x13)] // 005370be
    static void OpenedPetInfo(ref Req req, Client client) {
        // opened pet information
        // idk why this exists. maybe to update stats? but they should be up to date anyway
    }

    #endregion

    #region Response

    // 0D_01
    public static void SendPetData(Client client, int id, PetData pet) {
        var b = new PacketBuilder(0xD, 0x01);

        b.WriteByte((byte)(id + 1));

        b.BeginCompress();
        if(pet == null) {
            PetData.WriteEmpty(b);
        } else {
            b.Write(pet);
        }
        b.EndCompress();

        b.Send(client);
    }

    // 0D_02
    public static void SendAddPetEnt(IEnumerable<Client> client, PetEntData petEnt) {
        var b = new PacketBuilder(0xD, 0x02);

        b.WriteCompressed(petEnt);
        b.Send(client);
    }

    // 0D_03
    public static void SendRemovePet(IEnumerable<Client> client, short ownerId) {
        var b = new PacketBuilder(0xD, 0x03);

        b.WriteShort(ownerId);

        b.Send(client);
    }

    // 0D_04
    public static void SendSetActivePet(Client client, int id) {
        var b = new PacketBuilder(0xD, 0x04);

        b.WriteByte((byte)id);

        b.Send(client);
    }

    // 0D_06
    public static void SendAnimationState(IEnumerable<Client> clients, short ownerId, byte state) {
        var b = new PacketBuilder(0x0D, 0x06);

        b.WriteShort(ownerId); // pet entity id

        // 1 = idle
        // 2 = walking
        // 7 = tired | hunger < 21%
        // 9 = barking | hunger < 11%
        b.WriteByte(state);

        b.Send(clients);
    }

    // 0D_07
    public static void SendMovePet(IEnumerable<Client> clients, short ownerId, int x, int y) {
        var b = new PacketBuilder(0xD, 0x07);

        b.WriteShort(ownerId);
        b.WriteInt(x);
        b.WriteInt(y);
        b.WriteInt(0); // unused

        b.Send(clients);
    }

    // 0D_0A
    // send the pets of all other players to client
    public static void SendAllPet(Client client, Client[] others) {
        var b = new PacketBuilder(0xD, 0x0A);

        b.WriteShort((short)others.Count(x => x.Player.ActivePet != -1));

        b.BeginCompress();
        foreach(var other in others) {
            if(other.Player.ActivePet != -1) {
                b.Write(other.Player.Pets[other.Player.ActivePet].EntData(other));
            }
        }
        b.EndCompress();

        b.Send(client);
    }

    // 0D_0B
    public static void SendUpdatePetEnt(IEnumerable<Client> clients, PetEntData pet) {
        var b = new PacketBuilder(0xD, 0x0B);

        b.WriteShort(pet.OwnerId);
        b.WriteCompressed(pet);

        b.Send(clients);
    }

    // 0D_0C
    // displays the message "The pet is currently clean and hygienic."
    public static void SendPetIsHygienic(Client client) {
        var b = new PacketBuilder(0xD, 0x0C);
        b.Send(client);
    }

    // 0D_0E
    public static void SendSetEatingCooldown(Client client, int id, int cooldown) {
        var b = new PacketBuilder(0xD, 0x0E);

        b.WriteByte((byte)(id + 1));
        b.WriteInt(cooldown); // in seconds

        b.Send(client);
    }

    // 0D_0F
    public static void SendSetHunger(Client client, int pet) {
        var b = new PacketBuilder(0xD, 0x0F);

        b.WriteByte((byte)(pet + 1));
        b.WriteByte((byte)client.Player.Pets[pet].Hunger); // hunger
        b.WriteByte(0); // field16_0x3e

        b.Send(client);
    }

    // 0D_10
    public static void SendSetPettingCooldown(Client client, int cooldown) {
        var b = new PacketBuilder(0xD, 0x10);
        // ((*global_gameData)->data).playerData.petData[id].petting_cooldown = cooldown;

        b.WriteByte((byte)(client.Player.ActivePet + 1));
        b.WriteInt(cooldown);

        b.Send(client);
    }

    // 0D_11
    public static void SendSetComfort(Client client, int pet) {
        var b = new PacketBuilder(0xD, 0x11);

        b.WriteByte((byte)(pet + 1));
        b.WriteByte((byte)client.Player.Pets[pet].Comfort); // comfort
        b.WriteByte(0); // field25_0xc7

        b.Send(client);
    }

    // 0D_12
    public static void SendUpdateStats(Client client) {
        var b = new PacketBuilder(0xD, 0x12);

        b.WriteByte((byte)(client.Player.ActivePet + 1));
        var pet = client.Player.Pets[client.Player.ActivePet];

        b.WriteInt(pet.Atk);
        b.WriteInt(pet.Def);
        b.WriteInt(pet.Dodge);
        b.WriteInt(pet.Crit);
        b.WriteInt(pet.Hp);
        b.WriteInt(pet.Sta);

        b.Send(client);
    }

    // 0D_13
    public static void SendBreedResult(Client client, byte type, int petId, int p1, int p2) {
        var b = new PacketBuilder(0xD, 0x13);

        b.WriteByte(type); // result type 0-4
        b.WriteInt(petId); // breed result pet id

        // depends on result type
        b.WriteInt(p1); // param 1
        b.WriteInt(p2); // param 2

        b.Send(client);
    }

    // 0D_14 - something to do with emoticons
    public static void SendShowEmoji(IEnumerable<Client> clients, short petId, int emoji) {
        var b = new PacketBuilder(0xD, 0x14);
        Debug.Assert(1 <= emoji && emoji <= 10);

        //  1 = Happy "Either you, or someone else, just cleaned your pet or pet it."
        //  2 = Unclean? "Your pet needs to be cleaned."
        //  3 = Sad "This is the first sign that your pet is becoming unhappy. Take care of it as soon as possible, before it is too late."
        //  4 = Eating "Either you, or someone else, just fed your pet."
        //  6 = Hungry "Your pet is hungry."
        //  7 = Very Sad "This pet's comfort has dropped to zero and is in critical condition, being only moments from abandoning its owner. Instant action is necessary to save this pet."
        //  8 = "The pet is near two or more matched pets. This is a pet party!"
        //  9 = "The pet is near one other matched pet."
        // 10 = "This is the emote a pet will emit when it is close to unmatched pets. At this point, it is best to put it away, or move completely out of range."

        b.WriteShort(petId);
        b.WriteInt(emoji);

        b.Send(clients);
    }

    // 0D_15
    public static void SendSetDirty(Client client, int slot, bool isDirty) {
        var b = new PacketBuilder(0xD, 0x15);

        b.WriteByte((byte)(slot + 1));
        b.WriteByte(isDirty);

        b.Send(client);
    }

    // 0D_16
    public static void SendMatchedCount(Client client, int count) {
        var b = new PacketBuilder(0xD, 0x16);

        b.WriteInt(count);

        b.Send(client);
    }

    // 0D_17
    public static void SendOtherPetInfo(Client client, Client other) {
        var b = new PacketBuilder(0xD, 0x17);

        foreach(var pet in other.Player.Pets) {
            b.BeginCompress();
            if(pet == null)
                PetData.WriteEmpty(b);
            else
                b.Write(pet);
            b.EndCompress();
        }

        var idk = other.Player.ActivePet + 1;
        b.WriteByte((byte)idk);
        b.WriteByte((byte)other.Player.Levels[(int)Skill.General]);

        if(idk != 0) {
            var p = other.Player.Pet;
            b.WriteString(p.Atk.ToString(), 1);
            b.WriteString(p.Def.ToString(), 1);
            b.WriteString(p.Dodge.ToString(), 1);
            b.WriteString(p.Crit.ToString(), 1);
            b.WriteString(p.Hp.ToString(), 1);
            b.WriteString(p.Sta.ToString(), 1);
        }

        b.Send(client);
    }

    // 0D_18 - something to do with a progress bar
    // feeding progress bar
    public static void SendFeedingProgress(Client client, int action, int duration = 0) {
        var b = new PacketBuilder(0xD, 0x18);
        Debug.Assert(0 <= action && action <= 3);

        // action
        // 0 = Action cancelled
        // 1 = start
        // 2 = end
        // 3 = Action cancelled + cooldown reset?
        b.WriteByte((byte)action);
        b.WriteInt(duration); // in ms

        b.Send(client);
    }
    // 0D_19 - cleaning progress bar
    public static void SendCleaningProgress(Client client, byte action, short duration = 0) {
        var b = new PacketBuilder(0xD, 0x19);

        // action
        // 0 = Action cancelled
        // 1 = start
        // 2 = end
        b.WriteByte(action);
        b.WriteInt(duration);

        b.Send(client);
    }

    // 0D_1C - load temporary pet? maybe for carnival?
    public static void SendSetTempPet(Client client, short petId) {
        var b = new PacketBuilder(0xD, 0x1C);

        b.WriteShort(petId);

        b.Send(client);
    }

    #endregion
}
