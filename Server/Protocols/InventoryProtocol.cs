using System;
using System.IO;

namespace Server {
    class InventoryProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 00586fd2
                    Recieve_09_01(req, res, account.PlayerData);
                    break;
                // case 0x09_02: // 
                // case 0x09_03: // 
                case 0x06: // 
                    SplitItem(req, res, account.PlayerData);
                    break;
                // case 0x09_0F: // 
                // case 0x09_10: // 
                // case 0x09_11: // 
                case 0x20: // check item delivery available?
                    Recieve_09_20(req, res);
                    break;
                // case 0x09_21: // 
                // case 0x09_22: //

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 09_01
        static void Recieve_09_01(BinaryReader req, Stream res, PlayerData player) {
            var idk1 = req.ReadByte();
            var fromPos = req.ReadByte() - 1;

            var idk2 = req.ReadByte();
            var destPos = req.ReadByte() - 1;

            var from = player.Inventory[fromPos];
            var to = player.Inventory[destPos];
            if(to.Id == 0 || (to.Id == from.Id && to.Count + from.Count < 99)) {
                player.Inventory[fromPos] = new InventoryItem();

                player.Inventory[destPos].Id = from.Id;
                player.Inventory[destPos].Count += from.Count;

                SendSetItem(res, (byte)(fromPos + 1), player.Inventory[fromPos]);
                SendSetItem(res, (byte)(destPos + 1), player.Inventory[destPos]);
            } else {
                // fail
            }
        }

        // 09_06
        static void SplitItem(BinaryReader req, Stream res, PlayerData player) {
            var pos = req.ReadByte() - 1;
            var count = req.ReadByte();

            for(int i = 0; i < player.InventorySize; i++) {
                if(player.Inventory[i].Id != 0)
                    continue;

                player.Inventory[i].Id = player.Inventory[pos].Id;
                player.Inventory[i].Count = count;

                player.Inventory[pos].Count -= count;

                SendSetItem(res, (byte)(i + 1), player.Inventory[i]);
                SendSetItem(res, (byte)(pos + 1), player.Inventory[pos]);
                break;
            }
        }

        // 09_20
        static void Recieve_09_20(BinaryReader req, Stream res) {
            // Send09_20(res);
        }
        #endregion

        #region Response
        // 09_02
        public static void SendSetItem(Stream clientStream, byte index, InventoryItem item) {
            var b = new PacketBuilder();

            b.WriteByte(0x09); // first switch
            b.WriteByte(0x02); // second switch

            b.BeginCompress();
            b.Write(item);
            b.EndCompress();

            b.WriteByte(index); // inventory index

            b.Send(clientStream);
        }

        // 09_03
        public static void SendGetItem(Stream clientStream, byte index, InventoryItem item) {
            var b = new PacketBuilder();

            b.WriteByte(0x09); // first switch
            b.WriteByte(0x03); // second switch

            b.BeginCompress();
            b.Write(item);
            b.EndCompress();

            b.WriteByte(index); // inventory index
            b.WriteByte(1); // bool - display special message
            b.WriteInt(0); // if(item->id == 0) {lost item id} else {unused}

            b.Send(clientStream);
        }
        #endregion
    }
}