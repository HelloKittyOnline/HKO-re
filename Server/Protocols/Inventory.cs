using System;
using System.Diagnostics;
using Extractor;

namespace Server.Protocols;

static class Inventory {
    #region Request
    [Request(0x09, 0x01)] // 00586fd2
    static void MoveItem(ref Req req, Client client) {
        var fromInv = req.ReadByte();
        var fromPos = req.ReadByte() - 1;

        var toInv = req.ReadByte();
        var toPos = req.ReadByte() - 1;

        lock(client.Player) {
            var from = client.GetItem(fromInv, fromPos);
            var to = client.GetItem(toInv, toPos);
            from.MoveTo(to);
        }
    }

    [Request(0x09, 0x02)] // 00587048
    static void MoveToInv(ref Req req, Client client) {
        var a = req.ReadByte() - 1;

        lock(client.Player) {
            client.GetItem(InvType.Farm, a).MoveTo(InvType.Player);
        }
    }

    [Request(0x09, 0x03)] // 005870bc
    static void MoveToFarm(ref Req req, Client client) {
        var a = req.ReadByte() - 1;
        lock(client.Player) {
            client.GetItem(InvType.Player, a).MoveTo(InvType.Farm);
        }
    }

    [Request(0x09, 0x06)] // 0058714a
    static void SplitItem(ref Req req, Client client) {
        var pos = req.ReadByte() - 1;
        var count = req.ReadByte(); // min: 1 - max: entire stack
        if(count == 0)
            return;

        lock(client.Player) {
            var item = client.GetItem(InvType.Player, pos);
            if(item.Id == 0 || item.Count <= count)
                return;

            var inv = client.GetInv(InvType.Player);
            if(!inv.FindEmpty(out var empty)) // no empty slot
                return;

            item.Count -= count;
            empty.Item = new InventoryItem {
                Id = item.Id,
                Count = count,
            };

            item.SendUpdate(false);
            empty.SendUpdate(false);
        }
    }

    static bool GetBit(this byte[] data, int index) {
        return (data[index >> 3] & (1 << (index & 7))) != 0;
    }
    static void SetBit(this byte[] data, int index) {
        data[index >> 3] |= (byte)(1 << (index & 7));
    }

    [Request(0x09, 0x0F)] // 00587207
    static void UseItem(ref Req req, Client client) {
        var slot = req.ReadByte();
        var b = req.ReadInt16();

        var c = req.ReadInt32();
        var d = req.ReadInt32();
        var e = req.ReadInt32();

        lock(client.Player) {
            if(slot - 1 >= client.Player.InventorySize)
                return;

            var item = client.GetItem(InvType.Player, slot - 1);
            if(item.Id == 0)
                return;

            var itemData = Program.items[item.Id];

            switch(itemData.Type) {
                case ItemType.Fertilizer:
                    UseFertilizer(client, item, c, d);
                    break;
                case ItemType.Seed:
                    UseSeed(client, item, c, d);
                    break;
                case ItemType.Item_Guide:
                    UseItemGuide(client, item);
                    break;
                case ItemType.Pet_Food:
                    UsePetFood(client, item);
                    break;
                case ItemType.Card:
                    // the games code contains hints at other kinds of cards but they were not fully implemented so there's no need to check the card type
                    UsePetCard(client, item);
                    break;
                case ItemType.Building_Permit:
                    UseBuildingPermit(client, item);
                    break;
                case ItemType.Furniture:
                    UseFurniture(client, item);
                    break;
                case ItemType.Farm_Certificate:
                    UseFarmCertificate(client, item);
                    break;
                case ItemType.Watering_Can:
                    UseWateringCan(client, item, c, d);
                    break;
                case ItemType.Pet_Bag:
                    UsePetBag(client, item);
                    break;
            }
        }
    }

    static void UseFertilizer(Client client, ItemRef item, int y, int x) {
        var farm = (Server.Farm)client.Player.Map;
        // todo: check access rights?

        if(farm.Fertilization[x + y * 20] == 100)
            return;

        item.Remove(1);
        farm.Fertilization[x + y * 20] = 100;

        Farm.SendSetFertilizer(client, (byte)y, (byte)x, 100);
    }

    static void UseSeed(Client client, ItemRef item, int y, int x) {
        var farm = (Server.Farm)client.Player.Map;
        // todo: check access rights? / check for planting level

        var data = item.Item.Data;

        int i = x + y * 20;

        if(farm.Fertilization[i] == 0) // no fertilizer
            return;
        if(farm.Plants[i].SeedId != 0) // occupied
            return;

        var seed = Program.seeds[data.SubId];

        if(seed.Level > (farm.Level == 1 ? 0 : farm.Level) + 3 || client.Player.Levels[(int)Skill.Farming] < seed.Level) {
            Farm.Send03(client, 2, 0);
            return;
        }

        item.Remove(1);

        var p = farm.Plants[i] = new() {
            IsItem = false,
            SeedId = data.SubId,
            State = PlantState.Seed
        };
        farm.ActivePlants.Add(i);

        Farm.SendSetPlant(client, (byte)x, (byte)y, p);
        client.AddExpAction(Skill.Farming, seed.Level);
    }

    static void UseItemGuide(Client client, ItemRef item) {
        var prodRule = item.Item.Data.SubId;

        var prodData = Program.prodRules[prodRule];
        var skill = prodData.GetSkill();

        if(client.Player.Levels[(int)skill] < prodData.RequiredLevel) {
            SendUsedSkillBook(client, SkillUsedFlag.LevelNotMet, prodRule);
            return;
        }

        if(client.Player.ProductionFlags.GetBit(prodRule)) {
            SendUsedSkillBook(client, SkillUsedFlag.AlreadyKnow, prodRule);
            return;
        }

        client.Player.ProductionFlags.SetBit(prodRule);
        item.Remove(1);

        SendUsedSkillBook(client, SkillUsedFlag.Success, prodRule);
    }

    static void UseBuildingPermit(Client client, ItemRef item) {
        var farm = client.Player.Farm;
        if(farm.House.HouseId != 0) {
            return;
        }

        var _item = item.Item;
        var dat = _item.Data;

        var house = Program.buildings[item.Item.Data.SubId];

        if(farm.Level < house.RequiredFarmLevel) {
            return;
        }

        farm.House.Name = "";
        farm.House.BuildingPermit = _item.Id;
        farm.House.HouseState = 1;
        farm.House.BuildingItems.AsSpan().Clear();

        item.Remove(1);

        Farm.SendHouseData(client, farm.House);
        Farm.Send23(client);
    }

    static void UseFurniture(Client client, ItemRef item) {
        var dat = Program.furniture[item.Item.Data.SubId];

        if(client.Player.MapType != 4)
            return;

        var floor = (HouseFloor)client.Player.Map;

        if(dat.Type == FurnitureType.Object) {
            // handled in 0F_04
            return;
        }

        if(dat.Type == FurnitureType.Room) {
            switch(dat.Position) {
                case FurniturePosition.Floor:
                    floor.FloorId = dat.Id;
                    break;
                case FurniturePosition.Wall:
                    floor.WallId = dat.Id;
                    break;
                case FurniturePosition.Window:
                    floor.WindowId = dat.Id;
                    break;
                case FurniturePosition.Outside:
                    floor.ViewId = dat.Id;
                    break;
                default:
                    return;
            }

            item.Remove(1);
            Hompy.SendHouseComponent(client, dat.Position, dat.Id, (byte)floor.Number);
        }
    }

    static void UseWateringCan(Client client, ItemRef item, int y, int x) {
        var farm = (Server.Farm)client.Player.Map;

        if(farm.Watered[x + y * 20] != 0)
            return;

        item.Remove(1);
        farm.Watered[x + y * 20] = 100;

        Farm.SendSetWatered(client, (byte)y, (byte)x, 1);
    }

    static void UsePetCard(Client client, ItemRef item) {
        var pets = client.Player.Pets;
        var lvl = client.Player.Levels[(int)Skill.General];
        if(lvl < item.Item.Data.Level) {
            Player.SendMessage(client, Player.MessageType.You_cannot_use_this_item_level_requirement_not_met);
            return;
        }
        if(lvl < 10) {
            Player.SendMessage(client, Player.MessageType.You_cannot_use_pets_until_you_reach_level_10);
            return;
        }
        if(pets[0] != null && lvl < 20) { // 1 slot
            Player.SendMessage(client, Player.MessageType.You_can_equip_only_one_pet_at_your_current_level_An_extra_pet_slot_will_open_when_you_reach_level_20);
            return;
        }
        if(pets[0] != null && pets[1] != null && lvl < 30) { // 2 slots
            Player.SendMessage(client, Player.MessageType.You_can_equip_only_two_pets_at_your_current_level__An_extra_pet_slot_will_open_when_you_reach_level_30);
            return;
        }
        if(pets[0] != null && pets[1] != null && pets[2] != null) { // 3 slots
            Player.SendMessage(client, Player.MessageType.Pet_quota_is_full); // is this the right message?
            return;
        }

        // find first empty pet slot
        int slot = 0;
        while(slot < 3) {
            if(client.Player.Pets[slot] == null)
                break;
            slot++;
        }
        Debug.Assert(slot < 3);

        // use charges to store pet level
        var pet = new PetData(item.Id, item.Item.Charges);
        client.Player.Pets[slot] = pet;
        item.Remove(1);

        Pet.SendPetData(client, slot, pet);
    }

    static void UsePetFood(Client client, ItemRef item) {
        Pet.DoFeed(client, client, item);
    }

    static void UsePetBag(Client client, ItemRef item) {
        int gain;
        switch(item.Id) {
            case 86:
                gain = 2;
                break; // small bag
            case 87:
                gain = 4;
                break; // large bag
            default:
                return; // unreachable
        }

        var pet = client.Player.Pet;
        if(pet == null)
            return;

        var size = pet.Inventory.Length;
        var capacity = pet.Data.InvSize;
        if(size < capacity) {
            // the wiki says the item is wasted if the inventory is already maxed out but we'll just fix that
            item.Remove(1);
            var old = pet.Inventory;
            pet.Inventory = new InventoryItem[Math.Min(capacity, size + gain)];
            old.CopyTo(pet.Inventory, 0);
            SendSetPetInventoryUnlockedSlots(client, (byte)pet.Inventory.Length);
        }
    }

    static void UseFarmCertificate(Client client, ItemRef item) {
        var type = item.Item.Data.SubId;

        if(client.Player.OwnedFarms.Contains(type)) {
            return;
        }

        client.Player.OwnedFarms.Add(type);
        client.Player.OwnedFarms.Sort();
        item.Remove(1);
    }

    [Request(0x09, 0x10)] // 005872a6
    static void DeleteItem(ref Req req, Client client) {
        var slot = req.ReadByte() - 1;
        var inventory = req.ReadByte();

        lock(client.Player) {
            var item = client.GetItem(inventory, slot);
            if((item.Item.Data.Transferable & TransferFlag.NON_DROPPABLE) != 0) {
                Player.SendMessage(client, Player.MessageType.You_cannot_drop_this_item);
            } else {
                item.Clear();
            }
        }
    }

    [Request(0x09, 0x11)] // 0058731c
    private static void Recv11(ref Req req, Client client) {
        throw new NotImplementedException();
    }

    [Request(0x09, 0x20)] // 005873ea
    static void GetItemDelivery(ref Req req, Client client) {
        var items = Database.GetOrders(client.DiscordId);
        SendDeliveryItems(client, items);
    }

    [Request(0x09, 0x21)] // 00587492
    static void GetItemDeliveryItem(ref Req req, Client client) {
        var orderStr = req.ReadString();
        var itemId = req.ReadInt32();
        var orderId = req.ReadInt32();

        var order = Database.GetOrder(client.DiscordId, orderId);
        if(order == null)
            return;

        lock(client.Player) {
            if(client.AddItem(order.ItemId, 1, true)) {
                SendGotDelivery(client, "", orderId);
                Database.DeleteOrder(orderId);
            }
        }
    }

    [Request(0x09, 0x22)] // 0058751f // merge items?
    private static void Recv22(ref Req req, Client client) {
        var a = req.ReadInt32();
        var b = req.ReadByte();

        throw new NotImplementedException();
    }
    #endregion

    #region Response
    // 09_01
    public static void SendSetMoney(Client client) {
        var b = new PacketBuilder(0x09, 0x01); // second switch

        b.WriteInt(client.Player.Money);

        b.Send(client);
    }

    // 09_02
    public static void SendSetPlayerItem(Client client, InventoryItem item, byte index) {
        var b = new PacketBuilder(0x09, 0x02);

        b.WriteCompressed(item);
        b.WriteByte(index); // inventory index

        b.Send(client);
    }

    // 09_03
    public static void SendGetItem(Client client, InventoryItem item, byte index, bool displayMessage) {
        var b = new PacketBuilder(0x09, 0x03); // second switch

        b.WriteCompressed(item);
        b.WriteByte(index); // inventory index
        b.WriteByte(Convert.ToByte(displayMessage)); // display special message
        b.WriteInt(0); // if(item->id == 0) {lost item id} else {unused}

        b.Send(client);
    }

    // 09_05
    public static void SendSetFarmItem(Client client, InventoryItem item, byte index) {
        var b = new PacketBuilder(0x09, 0x05); // second switch

        b.WriteCompressed(item);
        b.WriteByte(index); // inventory index

        b.Send(client);
    }

    // 09_06
    public static void SendSetPetItem(Client client, InventoryItem item, byte pet_slot, byte index) {
        var b = new PacketBuilder(0x09, 0x06); // second switch

        b.WriteByte(pet_slot);
        b.WriteCompressed(item);
        b.WriteByte(index); // inventory index

        b.Send(client);
    }

    // 09_07

    // 09_0B
    public static void SendSetInventorySize(Client client) {
        var b = new PacketBuilder(0x09, 0x0B); // second switch

        b.WriteByte((byte)client.Player.InventorySize);

        b.Send(client);
    }

    // 09_0C
    public static void SendSetPetInventoryUnlockedSlots(Client client, byte slots) {
        var b = new PacketBuilder(0x09, 0x0C); // second switch

        b.WriteByte(slots);

        b.Send(client);
    }

    // 09_20
    static void SendDeliveryItems(Client client, OrderItem[] items) {
        var b = new PacketBuilder(0x09, 0x20);

        // very odd
        b.WriteInt(0); // string item count
        b.WriteInt(items.Length); // int    item count

        /*
        for (int i = 0; i < stringItemCount; i++) {
            b.WriteString("", 1);
            b.WriteString(0.ToString(), 1);
            b.WriteString(0.ToString(), 1);
        }*/

        for(var i = 0; i < items.Length && i < 50; i++) {
            var item = items[i];
            b.WriteInt(item.Id);
            b.WriteInt(item.ItemId);
            b.WriteInt(0); // unused flag?
        }

        b.Send(client);
    }

    // 09_21
    static void SendGotDelivery(Client client, string orderName, int orderId) {
        var b = new PacketBuilder(0x09, 0x21);

        b.WriteByte(1); // success

        b.WriteString(orderName, 1);
        b.WriteInt(orderId);

        b.Send(client);
    }

    // 09_23
    public static void SendSetNormalTokens(Client client) {
        var b = new PacketBuilder(0x09, 0x23); // second switch

        b.WriteInt(client.Player.NormalTokens);

        b.Send(client);
    }

    // 09_24
    public static void SendSetSpecialTokens(Client client) {
        var b = new PacketBuilder(0x09, 0x24);

        b.WriteInt(client.Player.SpecialTokens);

        b.Send(client);
    }

    // 09_25
    public static void SendSetTickets(Client client) {
        var b = new PacketBuilder(0x09, 0x25);

        b.WriteInt(client.Player.Tickets);

        b.Send(client);
    }

    public enum SkillUsedFlag {
        Success = 1,
        LevelNotMet = 2,
        AlreadyKnow = 3,
        WrongItem = 4,
        Failed = 5
    }

    // 09_5B
    static void SendUsedSkillBook(Client client, SkillUsedFlag type, int prodId) {
        var b = new PacketBuilder(0x09, 0x5B); // second switch

        // 1 = Skill learned successfully
        // 2 = You cannot learn this skill: level requirement not met
        // 3 = You already know this skill
        // 4 = Wrong item for Skill Guide
        // 5 = Failed to use Skill Guide
        b.WriteByte((byte)type);

        if(type == SkillUsedFlag.Success) {
            b.WriteShort((short)(prodId / 512));
            b.WriteShort((short)(prodId % 512));
        }

        b.Send(client);
    }

    #endregion
}
