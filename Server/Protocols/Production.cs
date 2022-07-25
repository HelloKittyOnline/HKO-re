using System.Linq;
using System.Threading;

namespace Server.Protocols {
    static class Production {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01:
                    Recv01(client);
                    break; // 00529917
                case 0x04:
                    Recv04(client);
                    break; // 005299a6
                default:
                    client.LogUnknown(0x07, id);
                    break;
            }
        }

        static void Recv01(Client client) {
            var prod = client.ReadInt32();
            var action = client.ReadByte();
            // 1 = produce 1
            // 2 = produce all

            if(prod >= Program.prodRules.Length) {
                return;
            }

            var data = Program.prodRules[prod];
            var resItem = Program.items[data.ItemId];

            bool checkRequired() {
                return data.Ingredients.All(item => client.Player.HasItem(item.ItemId, item.Count));
            }

            if(!checkRequired()) {
                Send01(client, 1, 0);
                return;
            }

            var skill = data.GetSkill();

            const int productionTime = 5 * 1000;

            client.StartAction(token => {
                while(true) {
                    Send01(client, 3, productionTime);

                    Thread.Sleep(productionTime);
                    if(token.IsCancellationRequested)
                        break;

                    if(!checkRequired()) {
                        Send01(client, 1, 0);
                        break;
                    }

                    foreach(var item in data.Ingredients) {
                        client.RemoveItem(item.ItemId, item.Count);
                    }

                    client.AddItem(data.ItemId, data.Count);
                    client.Player.AddExpAction(client, skill, resItem.Level);

                    if(action == 1 || !checkRequired()) {
                        Send01(client, 7, 0);
                        break;
                    }
                }
            }, () => {
                Send01(client, 4, 0);
            });
        }

        static void Recv04(Client client) {
            var a = client.ReadByte();
            var b = client.ReadByte();
        }

        static void Send01(Client client, byte type, int time) {
            var b = new PacketBuilder();

            b.WriteByte(0x07); // first switch
            b.WriteByte(0x01); // second switch

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
            var b = new PacketBuilder();

            b.WriteByte(0x07); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteByte(0); // ((*global_gameData)->data).playerData.idk3

            b.Send(client);
        }
    }
}