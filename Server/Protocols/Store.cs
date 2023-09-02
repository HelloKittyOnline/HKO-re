using System.IO;
using System.Text.Json;
using Extractor;

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
    #region Request
    [Request(0x0B, 0x01)] // 0054d96c
    static void OpenStore(ref Req req, Client client) {
        var npcId = req.ReadInt32();

        if(!Program.Shops.TryGetValue(npcId, out var store)) {
            return;
        }

        SendStoreInfo(client, npcId, store);
    }

    [Request(0x0B, 0x02)] // 0054d9a4
    static void BuyItem(ref Req req, Client client) {
        var npcId = req.ReadInt32();
        var itemNum = req.ReadByte();

        if(!Program.Shops.TryGetValue(npcId, out var store))
            return;
        if(itemNum >= store.Items.Length)
            return;

        var item = store.Items[itemNum];

        var player = client.Player;
        lock(player) {
            switch(store.Type) {
                case ShopType.Money:
                    if(item.Price <= player.Money && client.AddItem(item.Id, item.Count, false)) {
                        client.Player.Money -= item.Price;
                        Inventory.SendSetMoney(client);
                    }
                    break;
                case ShopType.DreamFragments: {
                    var inv = client.GetInv(InvType.Player);
                    const int dreamFragmentId = 10204;
                    if(item.Price <= inv.GetItemCount(dreamFragmentId) && client.AddItem(item.Id, item.Count, false)) {
                        client.RemoveItem(dreamFragmentId, item.Price);
                    }
                    break;
                }
                case ShopType.TokenNormal:
                    if(item.Price <= player.NormalTokens && client.AddItem(item.Id, item.Count, false)) {
                        player.NormalTokens -= item.Price;
                        Inventory.SendSetNormalTokens(client);
                    }
                    break;
                case ShopType.TokenSpecial:
                    if(item.Price <= player.SpecialTokens && client.AddItem(item.Id, item.Count, false)) {
                        player.SpecialTokens -= item.Price;
                        Inventory.SendSetSpecialTokens(client);
                    }
                    break;
                case ShopType.Ticket:
                    if(item.Price <= player.Tickets && client.AddItem(item.Id, item.Count, false)) {
                        player.Tickets -= item.Price;
                        Inventory.SendSetTickets(client);
                    }
                    break;
            }
        }
    }

    [Request(0x0B, 0x03)] // 0054da30
    static void SellItem(ref Req req, Client client) {
        var npcId = req.ReadInt32();
        var itemSlot = req.ReadInt32() - 1;

        lock(client.Player) {
            var item = client.GetItem(InvType.Player, itemSlot);
            if(item.Id == 0)
                return;

            var itemData = item.Item.Data;

            if((itemData.Transferable & TransferFlag.NON_TRANSFERABLE_TO_MERCHANT) != 0) {
                Player.SendMessage(client, Player.MessageType.Failed_to_sell_item);
                return;
            }

            client.Player.Money += itemData.Price * item.Count;
            item.Clear();
            Inventory.SendSetMoney(client);
        }
    }
    #endregion

    #region Response
    // 0B_01
    static void SendStoreInfo(Client client, int npc, Shop store) {
        var b = new PacketBuilder(0x0B, 0x01);

        b.WriteInt(npc);

        b.WriteInt(store.Village);
        b.WriteByte((byte)store.Type);

        foreach(var item in store.Items) {
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
