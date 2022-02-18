using System;
using System.Threading;
using System.Threading.Tasks;
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
                    client.Logger.LogWarning($"Unknown Packet 06_{id}");
                    break;
            }
        }

        #region Request
        // 06_01
        static void Recieve_06_01(Client client) {
            // gathering

            var resId = client.ReadInt32();
            var idk2 = client.ReadByte(); // 1 or 2

            var table = Program.resources[resId].LootTable;

            // TODO: harvest time??
            const int harvestTime = 5 * 1000;

            if(table != 0) {
                var source = new CancellationTokenSource();
                client.Player.cancelSource = source;

                Task.Run(() => {
                    Thread.Sleep(harvestTime);
                    if(source.IsCancellationRequested)
                        return;

                    var item = Program.lootTables[table].GetRandom();
                    if(item != -1) {
                        client.AddItem(item, 1);
                    }
                });
            }

            Send06_01(client, harvestTime);
        }
        #endregion

        #region Request
        // 06_01
        static void Send06_01(Client client, int time) {
            var b = new PacketBuilder();

            b.WriteByte(0x06); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteByte(2);
            // 1  = "Item is being used"
            // 2  = ok?
            // 3  = "Cannot get resources right now"
            // 4  = "Your crafting/collection tool does not meet the level requirement: %s"
            // 5  = "You are not equipped with the right tools"
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