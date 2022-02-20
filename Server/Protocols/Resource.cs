using System;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    class Resource {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01: // 005a2513
                    Recieve_06_01(client);
                    break;
                default:
                    client.Logger.LogWarning($"Unknown Packet 06_{id:X2}");
                    break;
            }
        }

        #region Request
        // 06_01
        static void Recieve_06_01(Client client) {
            // gathering

            var resId = client.ReadInt32();
            var action = client.ReadByte(); // 1 or 2

            var resource = Program.resources[resId];

            // TODO: harvest time??
            const int harvestTime = 5 * 1000;

            var source = new CancellationTokenSource();
            client.Player.cancelSource = source;

            source.Token.Register(() => {
                Send06_01(client, 8, 0);
                client.Player.cancelSource = null;
            });

            Task.Run(() => {
                Thread.Sleep(harvestTime);
                if(source.IsCancellationRequested)
                    return;

                client.Player.cancelSource = null;

                var item = Program.lootTables[resource.LootTable].GetRandom();
                if(item != -1) {
                    client.AddItem(item, 1);
                }

                var type = action switch {
                    1 => resource.Type1,
                    2 => resource.Type2,
                    _ => throw new ArgumentOutOfRangeException()
                };

                client.Player.AddExpAction(client, type switch {
                    0 => Skill.Gathering,
                    1 => Skill.Mining,
                    2 => Skill.Woodcutting,
                    3 => Skill.Farming, // ?
                    _ => throw new ArgumentOutOfRangeException()
                }, resource.Level);

                Send06_01(client, 7, 0);
            });

            Send06_01(client, 2, harvestTime);
        }
        #endregion

        #region Response
        // 06_01
        static void Send06_01(Client client, byte type, int time) {
            var b = new PacketBuilder();

            b.WriteByte(0x06); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteByte(type);
            // 1  = "Item is being used"
            // 2  = start
            // 3  = "Cannot get resources right now"
            // 4  = "Your crafting/collection tool does not meet the level requirement: %s"
            // 5  = "You are not equipped with the right tools"
            // 6  = nothing?
            // 7  = end
            // 8  = "Action cancelled"
            // 9  = "You cannot use a Scrapped Tool to collect resources"
            // 10 = "You can't clean up the Pile of Litter because you didn't help shoo away the Litterbug"
            // 11 = "There are other litter piles to be cleaned up. Give others a chance to clean up as well"

            // if(prev == 4) itemId else harvestTime
            b.WriteInt(time);

            b.Send(client);
        }
        #endregion
    }
}