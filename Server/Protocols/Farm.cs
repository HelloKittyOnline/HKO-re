using Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Protocols;

static class Farm {
    #region Request

    [Request(0x0A, 0x01)] // 00581350
    private static void RequestEnterFarm(Client client) {
        var ownerId = client.ReadInt16();

        var farm = Program.clients.FirstOrDefault(x => x.Id == ownerId && x.InGame)?.Player.Farm;
        if(farm == null)
            return;

        if(ownerId != client.Id) {
            // TODO: send join request
            // Send20(farm.Owner, client.Player.Name);

            // deny entry for now
            Send01(client, 4);
            return;
        }

        // todo: check if player on valid map and near manager
        client.Player.ReturnMap = client.Player.CurrentMap;
        client.Player.CurrentMap = farm.Id;
        client.Player.PositionX = 576;
        client.Player.PositionY = 656;

        Player.SendChangeMap(client);
        SetDayTime(new[] { client }, (int)farm.DayTime.TotalMinutes / 10);
    }

    [Request(0x0A, 0x02)] // 005813c4
    private static void CancelEnterRequest(Client client) {
        var ownerId = client.ReadInt16();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x03)] // 0058148c
    private static void GatherPlant(Client client) {
        var y = client.ReadByte();
        var x = client.ReadByte();
        var action = client.ReadByte(); // 1 gather - 2 chop

        if(client.Player.Map is not Server.Farm farm)
            return;

        int index = x + y * 20;

        var plant = farm.Plants[index];
        if(plant.SeedId == 0 || plant.State == PlantState.None)
            return;

        var data = Program.seeds[plant.SeedId];
        var lvl = client.Player.GetToolLevel(action == 1 ? Skill.Gathering : Skill.Woodcutting);
        if(lvl < data.Level) {
            Send03(client, 3, 0);
            return; // not high enough level
        }

        const int harvestTime = 5 * 1000;

        client.StartAction(async token => {
            await Task.Delay(harvestTime);
            if(token.IsCancellationRequested)
                return;

            lock(farm) {
                var plant = farm.Plants[index];

                if(plant.SeedId == 0 || plant.State == PlantState.None)
                    return;

                if(action == 1) { // gather
                    if(plant.State != PlantState.FullyGrown)
                        return;

                    plant.HarvestCount++;
                    if(plant.HarvestCount % 30 == 0) { // wither
                        plant.State = PlantState.Withered;
                        SendSetPlant(client, x, y, plant);
                    }
                    if(plant.HarvestCount % 5 == 0) { // regress
                        plant.State = PlantState.Growing;
                        farm.ActivePlants.Add(index);
                        SendSetPlant(client, x, y, plant);
                    }

                    client.AddExpAction(Skill.Gathering, data.Level);
                    client.AddFromLootTable(data.GatherLoot);
                } else if(action == 2) {
                    if(plant.State == PlantState.Seed)
                        return;

                    plant.CutCount++;
                    if(plant.CutCount == 5) {
                        // destroy
                        farm.Plants[index] = new Plant();
                        farm.Fertilization[index] = 0;

                        SendSetPlant(client, x, y, plant);
                        SendSetFertilizer(client, x, y, 0);
                    }

                    client.AddExpAction(Skill.Woodcutting, data.Level);
                    client.AddFromLootTable(data.ChopLoot);
                }

                farm.Plants[index] = plant;
            }

            Send03(client, 4, 0); // stop
        }, () => {
            Send03(client, 6, 0); // cancel
        });

        Send03(client, 1, 0); // start
    }

    [Request(0x0A, 0x04)] // 00581504
    private static void GetFarmList(Client client) {
        var page = client.ReadInt16();

        SendFarmList(client, page, Program.clients.Where(x => x.InGame).Select(x => x.Player.Farm).ToArray());
    }

    [Request(0x0A, 0x05)] // 0058153c
    public static void GetFriendFarmList(Client client) {
        var page = client.ReadInt16();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x06)] // 005815b0
    public static void GetGuildFarmList(Client client) {
        var page = client.ReadInt16();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x07)] // 00581624
    public static void SearchFarms(Client client) {
        var note = client.ReadWString();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x08)] // 005816ac
    public static void KickPlayer(Client client) {
        var playerId = client.ReadInt32();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x09)] // 005817d4
    public static void AcknowledgeRequest(Client client) {
        var playerName = client.ReadWString();
        var allowed = client.ReadByte() != 0;

        throw new NotImplementedException();

        if(allowed) {
            // change map
        } else {
            // Send01
        }
    }

    [Request(0x0A, 0x0B)] // 00581810
    public static void OpenFarmManagement(Client client) {
        var idk = client.ReadInt32();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x0C)] // 00581884
    public static void ChangeFarmType(Client client) {
        var idk = client.ReadByte();
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x0E)] // 00581903
    private static void SetFarmName(Client client) {
        var name = client.ReadWString();
        client.Player.Farm.Name = name;
    }

    [Request(0x0A, 0x0F)] // 00581980
    public static void PauseFarm(Client client) {
        var pause = client.ReadByte() != 0; // true = pause, false = resume
        // never implemented?
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x16)] // 00581a0b
    public static void Recv16(Client client) { // something to do with giveitem
        var idk1 = client.ReadInt32();
        var idk2 = client.ReadInt32();

        throw new NotImplementedException();
    }

    [Request(0x0A, 0x18)] // 00581a6e // build house?
    public static void Recv18(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x19)] // 00581b48 // enter house?
    public static void Recv19(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x1A)] // 00581b84 // demolish house
    public static void Recv1A(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x1B)] // 00581be0 set house name?
    public static void Recv1B(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x1C)] // 00581c68 // "enter_gHouse" / "leave_gHouse"
    public static void Recv1C(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x24)] // 00581cdc // something building minigame
    public static void Recv24(Client client) {
        throw new NotImplementedException();
    }

    [Request(0x0A, 0x25)] // 00581d58 delete all plants
    public static void Recv25(Client client) {
        throw new NotImplementedException();
    }
    #endregion

    #region Response
    // 0A_01
    private static void Send01(Client client, byte message) {
        var b = new PacketBuilder(0x0A, 0x01);

        b.WriteByte(message);
        // 1 = You do not have a farm
        // 2 = The farm is locked
        // 3 = The farm is full

        b.Send(client);
    }

    // 0A_02
    public static void SendSetPlant(Client client, int x, int y, Plant plant) {
        var b = new PacketBuilder(0x0A, 0x02);

        b.WriteByte((byte)y);
        b.WriteByte((byte)x);

        b.BeginCompress();
        b.Write(plant);
        b.EndCompress();

        b.Send(client);
    }

    // 0A_03
    public static void Send03(Client client, byte message, int time) {
        var b = new PacketBuilder(0x0A, 0x03);

        b.WriteByte(message);
        // 1 = start
        // 2 = You do not meet the level requirement
        // 3 = You are not equipped with the right tools
        // 6 = Action cancelled
        // 7 = You can only have a maximum of %d of this special plant on your farm at any time
        b.WriteInt(time);

        b.Send(client);
    }

    // 0A_04
    private static void SendFarmList(Client client, int page, Span<Server.Farm> farms) {
        var b = new PacketBuilder(0x0A, 0x04);

        b.WriteShort((short)farms.Length); // total farms
        b.WriteShort(0); // unused

        page--;
        var c = Math.Min(9, farms.Length - page * 9);
        b.WriteShort((short)c); // number of farms on current page

        for(int i = 0; i < c; i++) {
            var farm = farms[i + page * 9];

            b.BeginCompress();

            b.WritePadWString(farm.OwnerName, 0x20 * 2);
            b.WritePadWString(farm.Name, 0x40 * 2);
            b.WriteByte(farm.Type);
            b.WriteByte(farm.Level);
            b.WriteShort(farm.OwnerId);

            b.EndCompress();
        }

        b.Send(client);
    }

    // 0A_06 - set all fertilizer

    // 0A_07
    public static void SendSetFertilizer(Client client, byte y, byte x, byte value) {
        var b = new PacketBuilder(0x0A, 0x07);

        b.WriteByte(y);
        b.WriteByte(x);
        b.WriteByte(value);

        b.Send(client);
    }

    // 0A_08 - set all watered
    public static void UpdateWatered(IEnumerable<Client> client, Server.Farm farm) {
        var b = new PacketBuilder(0x0A, 0x08);
        b.EncodeCrazy(farm.Watered);
        b.Send(client);
    }

    // 0A_09
    public static void SendSetWatered(Client client, byte y, byte x, byte value, bool waterAll = false) {
        var b = new PacketBuilder(0x0A, 0x09);

        b.WriteByte(y);
        b.WriteByte(x);
        b.WriteByte(value);
        b.WriteByte(0); // unused
        b.WriteByte(waterAll);

        b.Send(client);
    }

    // 0A_0A - plant die notification

    // 00_1F
    public static void SetDayTime(IEnumerable<Client> client, int timePeriod) {
        var b = new PacketBuilder(0x0A, 0x1F);

        b.WriteInt(timePeriod);

        b.Send(client);
    }
    #endregion
}
