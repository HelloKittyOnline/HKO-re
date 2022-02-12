using System;
using System.IO;

namespace Server.Protocols {
    class Inventory {
        public static void Handle(Client client) {
            switch(client.ReadByte()) {
                case 0x01: // 00586fd2
                    MoveItem(client);
                    break;
                // case 0x09_02: // 
                // case 0x09_03: // 
                case 0x06: // 
                    SplitItem(client);
                    break;
                // case 0x09_0F: // 
                // case 0x09_10: // 
                // case 0x09_11: // 
                case 0x20: // check item delivery available?
                    Recieve_09_20(client);
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
        static void MoveItem(Client client) {
            var idk1 = client.ReadByte();
            var fromPos = client.ReadByte() - 1;

            var idk2 = client.ReadByte();
            var destPos = client.ReadByte() - 1;

            var player = client.Player;

            var from = player.Inventory[fromPos];
            var to = player.Inventory[destPos];
            if(to.Id == 0 || (to.Id == from.Id && to.Count + from.Count < 99)) {
                player.Inventory[fromPos] = new InventoryItem();

                player.Inventory[destPos].Id = from.Id;
                player.Inventory[destPos].Count += from.Count;

                SendSetItem(client.Stream, player.Inventory[fromPos], (byte)(fromPos + 1));
                SendSetItem(client.Stream, player.Inventory[destPos], (byte)(destPos + 1));
            } else {
                // fail
            }
        }

        // 09_06
        static void SplitItem(Client client) {
            var pos = client.ReadByte() - 1;
            var count = client.ReadByte();

            var player = client.Player;

            for(int i = 0; i < player.InventorySize; i++) {
                if(player.Inventory[i].Id != 0)
                    continue;

                player.Inventory[i].Id = player.Inventory[pos].Id;
                player.Inventory[i].Count = count;

                player.Inventory[pos].Count -= count;

                SendSetItem(client.Stream, player.Inventory[i], (byte)(i + 1));
                SendSetItem(client.Stream, player.Inventory[pos], (byte)(pos + 1));
                break;
            }
        }

        // 09_20
        static void Recieve_09_20(Client client) {
            // Send09_20(res);
        }
        #endregion

        #region Response
        // 09_02
        public static void SendSetItem(Stream clientStream, InventoryItem item, byte index) {
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
        public static void SendGetItem(Stream clientStream, InventoryItem item, byte index, bool displayMessage) {
            var b = new PacketBuilder();

            b.WriteByte(0x09); // first switch
            b.WriteByte(0x03); // second switch

            b.BeginCompress();
            b.Write(item);
            b.EndCompress();

            b.WriteByte(index); // inventory index
            b.WriteByte(Convert.ToByte(displayMessage)); // display special message
            b.WriteInt(0); // if(item->id == 0) {lost item id} else {unused}

            b.Send(clientStream);
        }
        #endregion
    }
}