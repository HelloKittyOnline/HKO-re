using Extractor;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Server;

enum PlantState {
    None,
    Seed,
    Growing,
    FullyGrown,
    Withered
}

struct Plant : IWriteAble {
    public int SeedId { get; init; }
    public bool IsItem { get; init; }

    public TimeSpan LiveTime { get; set; }
    public PlantState State { get; set; } // 3 = done?
    public byte CutCount { get; set; } // max 5
    public byte HarvestCount { get; set; } // max 15?
    public int DaysWithoutWater { get; set; }

    public void Write(PacketBuilder b) {
        var data = Program.seeds[SeedId];

        b.WriteInt(SeedId);
        if(IsItem) {
            throw new NotImplementedException();
            b.WriteShort(0);
        } else {
            b.WriteShort((short)data.PlantAppearanceId);
        }
        b.WriteByte((byte)data.Level);
        b.Write0(5);
        b.WriteByte((byte)State);
        b.Write0(2);
        b.WriteByte((byte)(5 - CutCount));
        b.Write0(2);
        b.WriteByte(IsItem);
        b.Write0(9);
    }
}

class Farm : Instance, IWriteAble {
    public string Name { get; set; } = "";
    public byte Type { get; set; } = 1;
    public Plant[] Plants { get; set; }
    public byte[] Fertilization { get; set; }
    public byte[] Watered { get; set; } // i = x + y * 20

    public TimeSpan DayTime { get; set; }

    public InventoryItem[] Inventory { get; set; }

    /// <summary>
    /// indices of plants which need to be processed
    /// </summary>
    [JsonIgnore] 
    public List<int> ActivePlants = new();

    [JsonIgnore]
    public Client Owner {
        get => _owner;
        set {
            _owner = value;
            Id = 30000 + Owner.Id * 10; // any id > 30000 and id % 10 == 0 is a farm
        }
    }
    [JsonIgnore] private Client _owner;

    [JsonIgnore] public string OwnerName => Owner.Player.Name;
    [JsonIgnore] public short OwnerId => Owner.Id;

    [JsonIgnore] public byte Level => (byte)Program.farms[Type].Level;

    [JsonIgnore] public override IReadOnlyCollection<NpcData> Npcs => Array.Empty<NpcData>();
    [JsonIgnore] public override IReadOnlyCollection<MobData> Mobs => Array.Empty<MobData>();
    [JsonIgnore] public override IReadOnlyCollection<Teleport> Teleporters => Array.Empty<Teleport>();
    [JsonIgnore] public override IReadOnlyCollection<Resource> Resources => Array.Empty<Resource>();
    [JsonIgnore] public override IReadOnlyCollection<Checkpoint> Checkpoints => Array.Empty<Checkpoint>();

    public void Init() {
        Plants ??= new Plant[20 * 20];
        Fertilization ??= new byte[20 * 20];
        Watered ??= new byte[20 * 20];
        Inventory ??= new InventoryItem[200];

        for(int i = 0; i < Plants.Length; i++) {
            if(Plants[i].SeedId != 0) {
                ActivePlants.Add(i);
            }
        }
    }

    public void Write(PacketBuilder b) {
        var data = Program.farms[Type];

        // 12280 bytes
        b.WritePadWString(Name, 88 * 2);
        b.WriteByte(Type);
        b.WriteByte(Level);
        b.WriteByte(0);
        b.WriteByte(0); // paused
        b.Write0(8);
        b.WriteByte(9); // size
        b.WriteByte(0);
        b.WriteByte(0);
        b.WriteByte(0);

        for(int i = 0; i < 20 * 20; i++) {
            b.Write(Plants[i]);
        }

        b.Write(Fertilization);
        b.Write(Watered);

        b.Write0(0x2ff8 - 0x2FA0);
    }

    // TODO: change to one Task per farm
    public static async void FarmThread() {
        var lastTime = DateTimeOffset.UtcNow;
        while(true) {
            var now = DateTimeOffset.UtcNow;
            var passed = now - lastTime;
            lastTime = now;

            foreach(var client in Program.clients) {
                if(!client.InGame)
                    continue;

                ProcFarm(client, passed);
            }

            // todo: optimization: calculate wait time based on shortest plant / suspend if no plants active
            await Task.Delay(1000);
        }
    }

    private static void UpdateAsync(Client client, int i, Plant plant) {
        Task.Run(() => Protocols.Farm.SendSetPlant(client, i % 20, i / 20, plant));
    }

    private static void ProcFarm(Client client, TimeSpan passed) {
        // todo: figure out correct time value
        var growStep = TimeSpan.FromSeconds(30);
        var dayLength = TimeSpan.FromMinutes(50);

        var farm = client.Player.Farm;
        var isOnFarm = client.Player.Map == farm;
        if(!isOnFarm) {
            passed *= 0.25f; // slow down if not on farm
        }

        lock(farm) {
            var lastStep = (int)farm.DayTime.TotalMinutes / 10;
            farm.DayTime += passed;
            if(farm.DayTime > dayLength) { // if day ist over
                farm.DayTime -= dayLength;

                // update plant wither status - plant withers after 2 days without water
                for(int i = 0; i < farm.Plants.Length; i++) {
                    if(farm.Watered[i] == 100) {
                        farm.Watered[i] = 0;
                        Protocols.Farm.SendSetWatered(client, (byte)(i / 20), (byte)(i % 20), 0);
                        continue;
                    }

                    ref var plant = ref farm.Plants[i];
                    if(plant.SeedId == 0)
                        continue;

                    // todo send warning message
                    plant.DaysWithoutWater++;
                    if(plant.DaysWithoutWater == 2) {
                        plant.State = PlantState.Withered;
                        farm.ActivePlants.Remove(i);

                        if(isOnFarm)
                            UpdateAsync(client, i, plant);
                    }
                }

                // batching does not work because images are not cleared
                // farm.Watered.AsSpan().Clear();
                // Protocols.Farm.UpdateWatered(farm.Players, farm);
            }
            var newStep = (int)farm.DayTime.TotalMinutes / 10;

            if(lastStep != newStep) {
                Protocols.Farm.SetDayTime(farm.Players, newStep);
            }

            // todo: optimize farm loop - maybe sleep until next event or something like that
            for(int i = 0; i < farm.ActivePlants.Count; i++) {
                var id = farm.ActivePlants[i];

                ref var plant = ref farm.Plants[id];
                if(plant.SeedId == 0) { // should never happen but just in case
                    client.Logger.LogWarning("[{userID}] removed invalid plant index", client.DiscordId);
                    farm.ActivePlants.RemoveAt(i);
                    i--;
                    continue;
                }
                plant.LiveTime += passed;

                if(plant.LiveTime < growStep) {
                    continue;
                }

                plant.LiveTime = TimeSpan.Zero;
                if(plant.State == PlantState.Seed) {
                    plant.State = PlantState.Growing;

                    if(isOnFarm)
                        UpdateAsync(client, id, plant);
                } else if(plant.State == PlantState.Growing) {
                    plant.State = PlantState.FullyGrown;
                    farm.ActivePlants.RemoveAt(i);
                    i--;

                    if(isOnFarm)
                        UpdateAsync(client, id, plant);
                } else {
                    farm.ActivePlants.RemoveAt(i); // probably withered
                    i--;
                }
            }
        }
    }

    public void WriteLocked(PacketBuilder b) {
        // todo: add patterns for later farms

        // is unlocked
        var pattern = new byte[9, 9] {
            { 0, 1, 0, 1, 0, 1, 0, 1, 0 },
            { 1, 0, 1, 0, 0, 0, 1, 0, 1 },
            { 0, 1, 0, 1, 0, 1, 0, 1, 0 },
            { 1, 0, 1, 0, 0, 0, 1, 0, 1 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 0, 1, 0, 0, 0, 1, 0, 1 },
            { 0, 1, 0, 1, 0, 1, 0, 1, 0 },
            { 1, 0, 1, 0, 0, 0, 1, 0, 1 },
            { 0, 1, 0, 1, 0, 1, 0, 1, 0 },
        };

        for(int y = 0; y < 20; y++) {
            for(int x = 0; x < 20; x++) {
                b.WriteByte((x < 9 && y < 9) ? pattern[x, y] : (byte)0);
            }
        }
    }
}
