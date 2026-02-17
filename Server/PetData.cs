using Extractor;
using Server.Protocols;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server;

class PetData : IWriteAble {
    [JsonPropertyName("CardId")]
    public int CardItemId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }

    public int Comfort { get; set; }
    public int Hunger { get; set; }

    public InventoryItem[] Inventory { get; set; }

    [JsonIgnore] public int DirtyState { get; set; }

    // should this even be cached?
    [JsonIgnore] public int Hp;
    [JsonIgnore] public int Sta;
    [JsonIgnore] public int Atk;
    [JsonIgnore] public int Def;
    [JsonIgnore] public int Crit;
    [JsonIgnore] public int Dodge;

    [JsonIgnore] public PetInitData Data => Program.petInitData[PetId];
    [JsonIgnore] public int PetId => Program.items[CardItemId].SubId;

    public PetData() { }

    public PetData(int id, int level) {
        CardItemId = id;
        Name = Data.Name;
        Level = Math.Clamp(level, 1, 20);

        var capacity = Data.InvSize;
        if(capacity == 0) {
            Inventory = [];
        } else if(capacity == 5) {
            Inventory = new InventoryItem[2]; // 3 locked slots
        } else if(capacity == 10) {
            Inventory = new InventoryItem[3]; // 7 locked slots
        } else {
            Debugger.Break(); // should be unreachable
        }
        Comfort = 120; // half full

        calcStats();
    }

    public void calcStats() {
        Debug.Assert(CardItemId != 0);

        var data = Data;

        Hp = data.Hp + 4 * (Level - 1);
        Sta = data.Stamina + 4 * (Level - 1);
        Atk = data.Attack + 2 * (Level - 1);
        Def = data.Defense + 2 * (Level - 1);
        Crit = data.CritChance + 15 * (Level - 1);
        Dodge = data.DodgeChance + 15 * (Level - 1);
    }

    public PetEntData EntData(Client client) {
        return new PetEntData {
            OwnerId = client.Id,
            PetId = (short)PetId,
            Name = Name,
            X = client.Player.PositionX,
            Y = client.Player.PositionY
        };
    }

    public void Write(ref PacketBuilder b) {
        b.WritePadWString(Name, 40);
        b.WriteShort((short)PetId);

        b.WriteShort(0); // unused

        b.WriteShort((short)Hp);
        b.WriteShort((short)Sta);
        b.WriteShort((short)Atk);
        b.WriteShort((short)Def);
        b.WriteShort((short)Crit);
        b.WriteShort((short)Dodge);

        b.WriteShort(0); // unused

        b.WriteByte((byte)Hunger);
        b.WriteByte(0);
        b.WriteByte((byte)Level); // level
        b.WriteByte(0);
        b.WriteByte(0);
        b.WriteByte(0);

        b.WriteInt(Exp);
        b.WriteInt(0); // eating_cooldown
        b.WriteInt(CardItemId);

        for(int i = 0; i < Inventory.Length; i++)
            b.Write(Inventory[i]);
        for(int i = Inventory.Length; i < 10; i++)
            b.Write(new InventoryItem());

        b.WriteByte((byte)Data.InvSize); // capacity
        b.WriteByte((byte)Inventory.Length); // unlocked slots
        b.WriteByte((byte)Comfort);
        b.WriteByte(0);
        b.WriteByte(DirtyState == 2); // isDirty
        b.WriteByte(0);
        b.WriteByte(0);
        b.WriteByte(0);

        b.WriteInt(0); // pettingCooldown
        b.WriteInt(0);

        b.WriteByte(0);
        b.WriteByte(0);
        b.WriteByte(0);
        b.WriteByte(0);
    }

    public static void WriteEmpty(PacketBuilder b) {
        b.Write0(0xd8);
    }

    public void AddExp(int amount, Client client) {
        Exp += amount;

        var expCap = Program.petExp[Level].Exp;

        if(Exp > expCap) {
            if(Level < 20) { // data has 50 values but 20 is cap
                Exp -= expCap; // carry over excess exp
                Level++;

                calcStats();
                client.UpdateStats();
            } else {
                Exp = expCap;
            }
        }
        Pet.SendPetData(client, client.Player.ActivePet, this);
    }

    public static async Task PetTask(Client client) {
        bool odd = false;

        while(client.InGame) {
            await Task.Delay(60 * 1000); // wait 1 minute

            lock(client.Player) {
                for(int i = 0; i < 3; i++) {
                    var p = client.Player.Pets[i];
                    if(p == null)
                        continue;

                    if(client.Player.ActivePet == i) {
                        // do before updateing comfort for some buffer time
                        if(p.Comfort == 0 && Random.Shared.Next(100) < 10) {
                            // 10% every minute to run away

                            client.Player.Pets[i] = null;
                            client.Player.ActivePet = -1;
                            Pet.SendSetActivePet(client, 0);
                            Pet.SendPetData(client, i, null);
                            Pet.SendRemovePet(client.Player.Map.Players, client.Id);
                            Player.SendMessage(client, Player.MessageType.Your_pet_has_abandoned_you_as_a_result_of_neglect);
                            continue;
                        }

                        if(odd) // 1x every 2 minutes
                            p.Comfort = Math.Clamp(p.Comfort - 1, 0, 240);
                        p.Hunger = Math.Max(0, p.Hunger - 1); // 1 every minute

                        // emoji update
                        if(p.Comfort == 0) {
                            Pet.SendShowEmoji(client.Player.Map.Players, client.Id, 7);
                        } else if(p.Comfort < 24) { // < 10%
                            Pet.SendShowEmoji(client.Player.Map.Players, client.Id, 3);
                        } else if(p.Hunger < 24) { // < 10%
                            Pet.SendShowEmoji(client.Player.Map.Players, client.Id, 6);
                        } else if(p.DirtyState == 2) {
                            Pet.SendShowEmoji(client.Player.Map.Players, client.Id, 2);
                        }
                    } else {
                        if(odd) // 1 every 2 minutes
                            p.Hunger = Math.Max(0, p.Hunger - 1);
                    }

                    Pet.SendSetComfort(client, i);
                    Pet.SendSetHunger(client, i);
                }
            }

            odd = !odd;
        }
    }
}
