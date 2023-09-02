using System.Collections.Generic;
using Extractor;

namespace Server.Protocols;

static class Hompy {
    #region Request
    [Request(0x0F, 0x02)] // 00511e18
    public static void EnterDecorationMode(ref Req req, Client client) {
        var idk = req.ReadByte();
        SetDecorationMode(client, 1);
    }

    [Request(0x0F, 0x03)] // 00511e8c
    public static void ExitDecorationMode(ref Req req, Client client) {
        var idk = req.ReadByte();
        SetDecorationMode(client, 0);
    }

    [Request(0x0F, 0x04)] // 00511f75
    public static void PlaceFurniture(ref Req req, Client client) {
        var floor_ = req.ReadByte();
        var invSlot = req.ReadByte();
        var x = req.ReadInt32();
        var y = req.ReadInt32();
        var rotation = req.ReadByte();
        var idk6 = req.ReadByte(); // furniture slot ?
        var idk7 = req.ReadInt32();
        var idk8 = req.ReadInt32();

        // Console.WriteLine($"{floor_} {invSlot} {x} {y} {rotation} {idk6} {idk7} {idk8}");

        if(client.Player.MapType != 4)
            return;

        var item = client.GetItem(InvType.Player, invSlot - 1);
        var dat = item.Item.Data;
        if(dat.Id == 0)
            return;
        if(dat.Type != ItemType.Furniture)
            return;

        var fur = Program.furniture[dat.SubId];
        if(fur.Type == FurnitureType.Room)
            return;

        var floor = (HouseFloor)client.Player.Map;
        var house = floor.House;

        if(house.Furniture.Count >= 200) {
            return;
        }

        var asd = new FurnitureItem {
            Id = dat.Id,
            X = x - fur.Offsets[rotation - 1].X,
            Y = y - fur.Offsets[rotation - 1].Y,
            Rotation = rotation,
            State = 1,
            Floor = floor_
        };
        var id = house.AddFurniture(asd);
        item.Remove(1);

        AddFurnitureItem(client, (byte)(id + 1), asd);
    }

    [Request(0x0F, 0x05)] // 00512053
    public static void MoveFurniture(ref Req req, Client client) {
        var floor = req.ReadByte();
        var id = req.ReadByte();
        var x = req.ReadInt32();
        var y = req.ReadInt32();
        var rotation = req.ReadByte();
        var idk6 = req.ReadInt32();
        var idk7 = req.ReadInt32();

        if(client.Player.MapType != 4)
            return;

        var house = ((HouseFloor)client.Player.Map).House;

        if(!house.Furniture.TryGetValue(id - 1, out var item))
            return;

        var itemDat = Program.items[item.Id];
        var dat = Program.furniture[itemDat.SubId];

        item.X = x - dat.Offsets[rotation - 1].X;
        item.Y = y - dat.Offsets[rotation - 1].Y;
        item.Rotation = rotation;

        house.Furniture[id - 1] = item;
        UpdateFurnitureItem(client, id, item);
    }

    [Request(0x0F, 0x06)] // 005120e6
    public static void RemoveFurniture(ref Req req, Client client) {
        var floor = req.ReadByte();
        var id = req.ReadByte();

        var house = client.Player.Farm.House;

        if(!house.Furniture.TryGetValue(id - 1, out var item))
            return;

        if(client.GetInv(InvType.Player).AddItem(item.Id, 1, false) ||
           client.GetInv(InvType.Farm).AddItem(item.Id, 1, false)) {
            house.Furniture.Remove(id - 1);
            RemoveFurnitureItem(client, id);
        }
    }

    [Request(0x0F, 0x07)] // 00512176
    public static void ChangeFurnitureState(ref Req req, Client client) {
        var id = req.ReadByte();
        var b = req.ReadByte();

        var house = client.Player.Farm.House;
        if(!house.Furniture.TryGetValue(id - 1, out var item))
            return;

        if(item.State != b) {
            item.State = b;
            house.Furniture[id - 1] = item;
            SendFurnitureState(client, id, b);
        }
    }

    [Request(0x0F, 0x09)] // 005121da
    public static void ToggleLockDoor(ref Req req, Client client) {
        // throw new NotImplementedException();
    }

    [Request(0x0F, 0x0A)] // 00512236
    public static void Recv0A(ref Req req, Client client) {
        // throw new NotImplementedException();
    }

    [Request(0x0F, 0x0B)] // 005122a4
    public static void Recv0B(ref Req req, Client client) {
        // throw new NotImplementedException();
    }

    [Request(0x0F, 0x0C)] // 0051239d
    public static void RemoveAllFurniture(ref Req req, Client client) {
        var house = client.Player.Farm.House;

        lock(house) {
            var inv = client.GetInv(InvType.Player);
            var fi = client.GetInv(InvType.Farm);

            List<int> removed = new();
            foreach(var (key, val) in house.Furniture) {
                // first try inv and then farm inv
                // if both fail stop
                if(inv.AddItem(val.Id, 1, false, false) || fi.AddItem(val.Id, 1, false, false))
                    removed.Add(key);
                else
                    break;
            }
            foreach(var i in removed) {
                house.Furniture.Remove(i);
            }
            SendRemoveAllFurniture(client, removed);
        }
    }

    // 0x0F, 0x0D: // 0051242c
    // 0x0F, 0x0E: // 005124f7
    #endregion

    #region Response

    // 0F_01
    // stuff like walls and floors
    public static void SendHouseComponent(Client client, FurniturePosition type, int val, byte floor) {
        var b = new PacketBuilder(0x0F, 0x01);

        b.WriteByte((byte)type);
        b.WriteInt(val);
        b.WriteByte(floor);

        b.Send(client);
    }

    // 0F_02
    public static void SetDecorationMode(Client client, byte idk) {
        var b = new PacketBuilder(0x0F, 0x02);

        b.WriteByte(idk);
        b.WriteByte(0);

        b.Send(client);
    }

    private static void writeCollision(PacketBuilder b) {
        b.BeginCompress();
        // TODO

        b.WriteByte(0);
        b.WriteByte(0);

        for(int i = 0; i < 16 * 20; i++) {
            b.WriteByte(1); // collision map?
        }

        b.EndCompress();
    }

    // 0F_04
    public static void AddFurnitureItem(Client client, byte index, FurnitureItem item) {
        var b = new PacketBuilder(0x0F, 0x04);

        b.WriteByte(index); // 1 based

        b.BeginCompress();
        b.Write(item);
        b.EndCompress();

        b.WriteByte(0); // unused
        writeCollision(b);

        b.Send(client);
    }

    // 0F_05
    public static void UpdateFurnitureItem(Client client, byte index, FurnitureItem item) {
        var b = new PacketBuilder(0x0F, 0x05);

        b.WriteByte(index); // 1 based

        b.BeginCompress();
        b.Write(item);
        b.EndCompress();

        b.WriteByte(0); // unused
        writeCollision(b);

        b.Send(client);
    }

    // 0F_06
    public static void RemoveFurnitureItem(Client client, byte index) {
        var b = new PacketBuilder(0x0F, 0x06);

        b.WriteByte(index); // 1 based
        b.WriteByte(0); // unused
        writeCollision(b);

        b.Send(client);
    }

    // 0F_07
    public static void SendFurnitureState(Client client, byte id, byte state) {
        var b = new PacketBuilder(0x0F, 0x07);

        b.WriteByte(id);
        b.WriteByte(state);

        b.Send(client);
    }

    // 0F_08
    // 0F_09

    // 0F_0A
    public static void SendRemoveAllFurniture(Client client, List<int> removedIds) {
        var b = new PacketBuilder(0x0F, 0x0A);

        b.WriteByte(0); // unused
        b.WriteByte(1); // start index
        b.WriteByte((byte)removedIds.Count);
        b.WriteByte(0); // unused
        b.WriteInt(client.Id); // farm owner

        b.BeginCompress();
        for(int i = 0; i < 50; i++)
            b.Write(client.Player.Inventory[i]);
        b.EndCompress();

        b.BeginCompress();
        for(int i = 0; i < 200; i++)
            b.Write(client.Player.Farm.Inventory[i]);
        b.EndCompress();

        writeCollision(b);

        b.BeginCompress();
        b.WriteByte(0); // placeholder because of 1 based arrays
        foreach(var id in removedIds) {
            b.WriteByte((byte)(id + 1));
        }
        b.EndCompress();

        b.Send(client);
    }

    // 0F_0B

    #endregion
}
