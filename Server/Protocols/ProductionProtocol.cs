using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    class ProductionProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 005a2513
                    Recieve_06_01(req, res, account.PlayerData);
                    break;
                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 06_01
        static void Recieve_06_01(BinaryReader req, Stream res, PlayerData player) {
            // gathering

            var resId = req.ReadInt32();
            var idk2 = req.ReadByte(); // 1 or 2

            var table = Program.resources[resId - 1].LootTable;

            // TODO: harvest time??
            const int harvestTime = 5 * 1000;

            if(table != 0) {
                var source = new CancellationTokenSource();
                player.cancelSource = source;

                Task.Run(() => {
                    Thread.Sleep(harvestTime);
                    if(source.IsCancellationRequested)
                        return;

                    var item = Program.lootTables[table - 1].GetRandom();
                    if(item != -1) {
                        var pos = player.AddItem(item);
                        if(pos == -1) {
                            // inventory full
                        } else {
                            InventoryProtocol.SendGetItem(res, (byte)(pos + 1), player.Inventory[pos]);
                        }
                    }
                });
            }

            Send06_01(res, harvestTime);
        }
        #endregion

        #region Request
        // 06_01
        static void Send06_01(Stream clientStream, int time) {
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

            b.Send(clientStream);
        }
        #endregion
    }
}