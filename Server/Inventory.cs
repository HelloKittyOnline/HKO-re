using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Extractor;
using Server.Protocols;

namespace Server;

struct InventoryItem : IWriteAble {
    public int Id { get; set; }
    public byte Count { get; set; }
    // public byte Durability { get; set; }

    // currently only used to store pet level
    public byte Charges { get; set; }

    [JsonIgnore] public readonly ItemAtt Data => Program.items[Id];

    public void Write(ref PacketBuilder w) {
        w.WriteInt(Id); // id

        w.WriteByte(Count); // count
        w.WriteByte(0); // durability - unused
        w.WriteByte(Charges); // charges
        w.WriteByte(0); // unused?

        w.WriteByte(0); // idk
        w.WriteByte(0); // idk
        w.WriteByte(0); // idk
        w.WriteByte(0); // flags
        // f & 1 = 
        // f & 2 = 
        // f & 4 = scrapped?
    }
}

enum InvType {
    Player = 1,
    Farm = 2,
    Pet = 3,

    Equipment = 256,
    Tool = 257
}

readonly ref struct InvRef {
    private readonly Client client;
    private readonly InvType type;
    private readonly Span<InventoryItem> inv;

    public ItemRef this[int index] => new(client, type, index, ref inv[index]);

    public InvRef(Client client, InvType type) {
        this.client = client;
        this.type = type;

        inv = this.type switch {
            InvType.Player => client.Player.Inventory.AsSpan(0, this.client.Player.InventorySize),
            InvType.Farm => client.Player.Farm.Inventory.AsSpan(),
            InvType.Pet => client.Player.Pet.Inventory.AsSpan(),
            InvType.Equipment => client.Player.Equipment.AsSpan(),
            InvType.Tool => client.Player.Tools.AsSpan(),
            _ => []
        };
    }

    /// <summary>Try to find an emtpy inventory slot</summary>
    public bool FindEmpty(out ItemRef item) {
        for(int i = 0; i < inv.Length; i++) {
            if(inv[i].Id == 0) {
                item = this[i];
                return true;
            }
        }

        item = new ItemRef();
        return false;
    }

    public bool AddFromLootTable(int lootTable) {
        var item = Program.lootTables[lootTable].GetRandom();
        return item != 0 && AddItem(item, 1, true);
    }
    public bool AddItem(int id, int count, bool notification, bool sendUpdate = true) {
        return AddItem(new InventoryItem { Id = id, Count = (byte)count, Charges = 0 }, notification, sendUpdate);
    }

    /// <summary>
    /// Adds item to this inventory
    /// </summary>
    /// <param name="item_">Item to add</param>
    /// <param name="notification">If true displays a message in chat</param>
    /// <param name="sendUpdate">Should an update be sent at all</param>
    /// <returns></returns>
    public bool AddItem(InventoryItem item_, bool notification, bool sendUpdate = true) {
        Debug.Assert(item_.Id != 0);
        Debug.Assert(item_.Count > 0 && item_.Count <= item_.Data.StackLimit);
        Debug.Assert(item_.Charges == 0 || item_.Data.StackLimit == 1, "Item with charges should not be stackable");

        // todo: implement adding with count > stacklimit

        if(!FindInsert(item_.Id, item_.Count, out var item)) {
            if(sendUpdate)
                Player.SendMessage(client, Player.MessageType.Inventory_full);
            return false;
        }

        if(item_.Data.Type == ItemType.Card) {
            // remember: if items are added in other ways this has to be replicated
            item.Item.Charges = Math.Max(item.Item.Charges, (byte)1); // set min level to 1
            client.Player.Cards.Add(item_.Data.SubId);
        }

        if(item.Id == 0) {
            item.Item = item_;
        } else {
            Debug.Assert(item.Id == item_.Id);
            Debug.Assert(item_.Charges == 0);
            Debug.Assert(item.Count + item_.Count <= item_.Data.StackLimit);
            item.Count += item_.Count;
        }

        if(sendUpdate)
            item.SendUpdate(notification);
        return true;

    }

    private bool FindInsert(int itemId, int count, out ItemRef item) {
        var limit = Program.items[itemId].StackLimit;

        if(limit > 1) {
            // find existing stack with enough capacity
            for(int i = 0; i < inv.Length; i++) {
                var _item = inv[i];
                if(_item.Id == itemId && _item.Count + count <= limit) {
                    item = this[i];
                    return true;
                }
            }
        }

        // no fitting item found make new stack
        for(int i = 0; i < inv.Length; i++) {
            if(inv[i].Id == 0) {
                item = this[i];
                return true;
            }
        }

        // no free space found
        item = new ItemRef();
        return false;
    }

    public bool RemoveItem(int itemId, int count) {
        // bug: removing quest requirement does not toggle dialog marker
        if(itemId == 0 || count == 0)
            return true;

        if(GetItemCount(itemId) < count)
            return false;

        for(int i = 0; i < inv.Length; i++) {
            if(inv[i].Id != itemId)
                continue;

            var el = this[i];
            var _count = el.Count;

            if(count < _count) {
                inv[i].Count -= (byte)count;
                el.SendUpdate(false);
                return true;
            }
            if(_count == count) {
                el.Clear();
                return true;
            }

            el.Clear();
            count -= _count;
        }

        // should never happen
        // could not remove all items
        Debug.Assert(false);
        return false;
    }

    public int GetItemCount(int itemId) {
        if(itemId == 0)
            return 0;
        int count = 0;
        foreach(var item in inv) {
            if(item.Id == itemId) {
                count += item.Count;
            }
        }
        return count;
    }
    public int FreeSlots() {
        int free = 0;
        foreach(var item in inv) {
            if(item.Id == 0)
                free++;
        }
        return free;
    }
}

readonly ref struct ItemRef {
    private readonly Client client;
    public readonly InvType type;
    public readonly int Index;
    public readonly ref InventoryItem Item;

    public readonly int Id => Item.Id;
    public byte Count {
        get => Item.Count;
        set => Item.Count = value;
    }

    public ItemRef(Client client, InvType type, int index, ref InventoryItem item) {
        this.client = client;
        this.type = type;
        Index = index;
        Item = ref item;
    }

    public bool MoveTo(InvType inv) {
        Debug.Assert(type != inv);

        if(Id == 0)
            return false;

        if(client.GetInv(inv).AddItem(Item, false)) {
            Clear();
            return true;
        }

        return false;
    }

    public void Swap(ItemRef other) {
        (Item, other.Item) = (other.Item, Item);

        SendUpdate(false);
        other.SendUpdate(false);
    }

    public void Remove(int count) {
        Debug.Assert(count <= Item.Count);
        if(count < Item.Count) {
            Item.Count -= (byte)count;
        } else {
            Item = new InventoryItem();
        }
        SendUpdate(false);
    }

    public void Clear() {
        Item = new InventoryItem();
        SendUpdate(false);
    }

    /// <summary>
    /// Send updated inventory slot to player
    /// </summary>
    /// <param name="notification">If true displays a message in chat</param>
    public void SendUpdate(bool notification) {
        switch(type) {
            case InvType.Player when notification:
                Inventory.SendGetItem(client, Item, (byte)(Index + 1), true);
                break;
            case InvType.Player:
                Inventory.SendSetPlayerItem(client, Item, (byte)(Index + 1));
                break;
            case InvType.Pet:
                Debug.Assert(client.Player.ActivePet != -1);
                Inventory.SendSetPetItem(client, Item, (byte)(client.Player.ActivePet + 1), (byte)(Index + 1));
                break;
            case InvType.Farm:
                Inventory.SendSetFarmItem(client, Item, (byte)(Index + 1));
                break;
            case InvType.Equipment:
                Player.SendSetEquItem(client, Item, (byte)(Index + 1), false);
                client.Player.UpdateEntities();
                Player.SendPlayerAtt(client);
                client.UpdateStats();
                break;
            case InvType.Tool:
                Player.SendSetEquItem(client, Item, (byte)(Index + 1), true);
                break;
        }
    }
}
