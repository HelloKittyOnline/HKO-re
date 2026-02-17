using System;
using System.Diagnostics;
using Extractor;
using Server.Protocols;

namespace Server;

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

    public ItemRef this[int index] => new ItemRef(client, type, index, ref inv[index]);

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

    public bool FindEmpty(out ItemRef item) {
        for(int i = 0; i < inv.Length; i++) {
            if(inv[i].Id == 0) {
                item = new ItemRef(client, type, i, ref inv[i]);
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
        Debug.Assert(count >= 0 && count < 256);
        return AddItem(new InventoryItem { Id = id, Count = (byte)count, Charges = 0 }, notification, sendUpdate);
    }

    public bool AddItem(InventoryItem item_, bool notification, bool sendUpdate = true) {
        Debug.Assert(item_.Count <= item_.Data.StackLimit);
        Debug.Assert(item_.Charges == 0 || item_.Data.StackLimit == 1);

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
            Debug.Assert(item.Count + item_.Count < item_.Data.StackLimit);
            item.Count += item_.Count;
        }

        if(sendUpdate)
            item.SendUpdate(notification);
        return true;

    }

    public bool FindInsert(int itemId, int count, out ItemRef item) {
        var limit = Program.items[itemId].StackLimit;

        if(limit > 1) {
            // find existing stack
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
            var _item = inv[i];
            if(_item.Id == 0) {
                item = this[i];
                return true;
            }
        }

        item = new ItemRef();
        return false;
    }

    public bool RemoveItem(int itemId, int count) {
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

ref struct ItemRef {
    private readonly Client client;
    public readonly InvType type;
    public readonly int Index;
    public ref InventoryItem Item;

    public int Id => Item.Id;
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

    public bool MoveTo(ItemRef to) {
        var a = Item;
        var b = to.Item;

        if(a.Id == 0)
            return false;

        if(b.Id == 0) {
            to.Item = a;
            Item = new InventoryItem();

            SendUpdate(false);
            to.SendUpdate(false);
            return true;
        }
        if(a.Id == b.Id && a.Count + b.Count <= a.Data.StackLimit) {
            to.Item = a with {
                Count = (byte)(a.Count + b.Count)
            };
            Item = new InventoryItem();

            SendUpdate(false);
            to.SendUpdate(false);
            return true;
        }

        return false;
    }
    public bool MoveTo(InvRef inv) {
        if(Id == 0)
            return false;

        if(!inv.FindInsert(Id, Count, out var empty)) {
            return false;
        }

        empty.Item = Item;
        Clear();
        empty.SendUpdate(false);

        return true;
    }
    public bool MoveTo(InvType inv) {
        return MoveTo(client.GetInv(inv));
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
