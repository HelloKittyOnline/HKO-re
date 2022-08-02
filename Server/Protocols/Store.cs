using System;
using System.IO;
using System.Text.Json;

namespace Server.Protocols; 

enum ShopType : byte {
    Money = 0,
    DreamFragments = 1,
    TokenNormal = 2,
    TokenSpecial = 3,
    Ticket = 4,
}

class ShopItem {
    public int Id { get; set; }
    public int Price { get; set; }
    public int Count { get; set; }
    public int Friendship { get; set; }
}

class Shop {
    public int[] Npcs { get; set; }
    public int Village { get; set; }
    public ShopType Type { get; set; }
    public ShopItem[] Items { get; set; }

    public static Shop[] Load(string path) {
        return JsonSerializer.Deserialize<Shop[]>(File.ReadAllText(path));
    }
}

static class Store {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            case 0x01: // 0054d96c
                OpenStore(client);
                break;
            case 0x02: // 0054d9a4
                BuyItem(client);
                break;
            case 0x03: // 0054da30
                SellItem(client);
                break;
            default:
                client.LogUnknown(0x0B, id);
                break;
        }
    }

    #region Request
    // 0B_01
    static void OpenStore(Client client) {
        var npcId = client.ReadInt32();

        if (!Program.Shops.TryGetValue(npcId, out var store)) {
            return;
        }

        if(store != null) {
            SendStoreInfo(client, npcId, store);
        }
    }

    // 0B_02
    static void BuyItem(Client client) {
        var npcId = client.ReadInt32();
        var itemNum = client.ReadByte();

        if(!Program.Shops.TryGetValue(npcId, out var store)) {
            return;
        }

        if(itemNum >= store.Items.Length) {
            throw new ArgumentOutOfRangeException(nameof(itemNum));
        }

        var item = store.Items[itemNum];

        var canAfford = item.Price <= store.Type switch {
            ShopType.Money => client.Player.Money,
            ShopType.TokenNormal => client.Player.NormalTokens,
            ShopType.TokenSpecial => client.Player.SpecialTokens,
            ShopType.Ticket => client.Player.Tickets,
            _ => throw new ArgumentOutOfRangeException(nameof(store.Type))
        };

        if(canAfford && client.AddItem(item.Id, item.Count)) {
            client.Player.Money -= item.Price;
            Inventory.SendSetMoney(client);
        }
    }

    // 0B_03
    static void SellItem(Client client) {
        var npcId = client.ReadInt32();
        var itemSlot = client.ReadInt32() - 1;

        if(itemSlot >= client.Player.InventorySize) {
            return;
        }

        var item = client.Player.Inventory[itemSlot];
        if(item.Id == 0) {
            return;
        }

        var itemData = Program.items[item.Id];

        client.Player.Inventory[itemSlot] = InventoryItem.Empty;
        Inventory.SendSetItem(client, InventoryItem.Empty, (byte)(itemSlot + 1));

        client.Player.Money += itemData.Price * item.Count;
        Inventory.SendSetMoney(client);
    }
    #endregion

    #region Response
    // 0B_01
    static void SendStoreInfo(Client client, int npc, Shop store) {
        var b = new PacketBuilder();

        b.WriteByte(0x0B); // first switch
        b.WriteByte(0x01); // second switch

        b.WriteInt(npc);

        b.WriteInt(store.Village);
        b.WriteByte((byte)store.Type);

        foreach (var item in store.Items) {
            b.WriteInt(item.Id);
            b.WriteInt(item.Count);
            b.WriteInt(item.Price);
            b.WriteInt(item.Friendship);
        }

        // pad to multiple of 50 because the game has bad code
        int left = 50 - store.Items.Length % 50;
        for(int i = 0; i < left; i++) {
            b.WriteInt(0);
        }
        b.WriteInt(-1); // end

        b.Send(client);
    }
    #endregion
}
