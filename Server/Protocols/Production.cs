using System;
using System.Linq;
using System.Threading;

namespace Server.Protocols;

static class Production {
    [Request(0x07, 0x01)] // 00529917
    static void ProduceItem(Client client) {
        var prod = client.ReadInt32();
        var produce1 = client.ReadByte() == 1;
        // 1 = produce 1
        // 2 = produce all

        if(prod >= Program.prodRules.Length) {
            return;
        }

        var data = Program.prodRules[prod];
        var resItem = Program.items[data.ItemId];

        var skill = data.GetSkill();
        var level = client.Player.Levels[(int)skill];

        if(level < resItem.Level) {
            return;
        }

        bool CheckRequired() {
            return data.Ingredients.All(item => item.ItemId == 0 || client.GetInv(InvType.Player).GetItemCount(item.ItemId) >= item.Count);
        }

        if(!CheckRequired()) {
            Send01(client, 1, 0);
            return;
        }


        const int productionTime = 5 * 1000;

        client.StartAction(token => {
            while(true) {
                Send01(client, 3, productionTime);

                Thread.Sleep(productionTime);
                if(token.IsCancellationRequested)
                    break;

                lock(client.Player) {
                    if(!CheckRequired()) {
                        Send01(client, 1, 0);
                        break;
                    }

                    foreach(var item in data.Ingredients) {
                        if(item.ItemId == 0)
                            continue;

                        client.RemoveItem(item.ItemId, item.Count);
                    }

                    client.AddItem(data.ItemId, data.Count, true);
                    client.AddExpAction(skill, resItem.Level);

                    if(produce1 || !CheckRequired()) {
                        Send01(client, 7, 0);
                        break;
                    }
                }
            }
        }, () => {
            Send01(client, 4, 0);
        });
    }

    [Request(0x07, 0x04)] // 005299a6
    static void Recv04(Client client) {
        var a = client.ReadByte();
        var b = client.ReadByte();

        throw new NotImplementedException();
    }

    static void Send01(Client client, byte type, int time) {
        var b = new PacketBuilder(0x07, 0x01);

        // 1 = You do not have enough materials
        // 2 = You do not have enough Action Points
        // 3 = Production started
        // 4 = Action cancelled
        // 5 = Rank too low
        // 6 = Internal data error
        // 7 = ?? plays a sound

        b.WriteByte(type);
        if(type == 3) {
            b.WriteInt(time);
        }

        b.Send(client);
    }

    static void Send04(Client client) {
        var b = new PacketBuilder(0x07, 0x04);

        b.WriteByte(0); // ((*global_gameData)->data).playerData.idk3

        b.Send(client);
    }
}
