using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    class Inventory {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01: // 00586fd2
                    MoveItem(client);
                    break;
                // case 0x09_02: // 00587048
                // case 0x09_03: // 005870bc
                case 0x06: // 0058714a
                    SplitItem(client);
                    break;
                case 0x0F: // 00587207
                    ConsumeItem(client);
                    break;
                case 0x10: // 005872a6
                    DeleteItem(client);
                    break;
                // case 0x09_11: // 0058731c
                case 0x20: // 005873ea check item delivery available?
                    Recieve_09_20(client);
                    break;
                // case 0x09_21: // 00587492
                // case 0x09_22: // 0058751f

                default:
                    client.Logger.LogWarning($"Unknown Packet 09_{id}");
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

                SendSetItem(client, player.Inventory[fromPos], (byte)(fromPos + 1));
                SendSetItem(client, player.Inventory[destPos], (byte)(destPos + 1));
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

                SendSetItem(client, player.Inventory[i], (byte)(i + 1));
                SendSetItem(client, player.Inventory[pos], (byte)(pos + 1));
                break;
            }
        }

        // 09_0F
        static void ConsumeItem(Client client) {
            var slot = client.ReadByte();
            var b = client.ReadInt16();
            
            var c = client.ReadInt32();
            var d = client.ReadInt32();
            var e = client.ReadInt32();
        }

        // 09_10
        static void DeleteItem(Client client) {
            var slot = client.ReadByte();
            var inventory = client.ReadByte();

            switch (inventory) {
                case 1:
                    client.Player.Inventory[slot - 1] = InventoryItem.Empty;
                    SendSetItem(client, InventoryItem.Empty, slot);
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
        public static void SendSetItem(Client client, InventoryItem item, byte index) {
            var b = new PacketBuilder();

            b.WriteByte(0x09); // first switch
            b.WriteByte(0x02); // second switch

            b.BeginCompress();
            b.Write(item);
            b.EndCompress();

            b.WriteByte(index); // inventory index

            b.Send(client);
        }

        // 09_03
        public static void SendGetItem(Client client, InventoryItem item, byte index, bool displayMessage) {
            var b = new PacketBuilder();

            b.WriteByte(0x09); // first switch
            b.WriteByte(0x03); // second switch

            b.BeginCompress();
            b.Write(item);
            b.EndCompress();

            b.WriteByte(index); // inventory index
            b.WriteByte(Convert.ToByte(displayMessage)); // display special message
            b.WriteInt(0); // if(item->id == 0) {lost item id} else {unused}

            b.Send(client);
        }
        #endregion
    }
}