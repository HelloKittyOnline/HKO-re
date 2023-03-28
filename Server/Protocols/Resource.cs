using System.Threading;

namespace Server.Protocols;

static class Resource {
    #region Request
    [Request(0x06, 0x01)] // 005a2513
    static void HarvestResource(Client client) {
        // gathering

        var resId = client.ReadInt32();
        var action = client.ReadByte(); // 1 or 2

        var resource = Program.resources[resId];

        var skill = resource.GetSkill(action);
        var level = client.Player.Levels[(int)skill];
        var toolLevel = client.Player.GetToolLevel(skill);

        if(level < resource.Level || toolLevel < resource.Level) {
            Send06_01(client, 4, 0);
            return;
        }
        
        // TODO: harvest time??
        const int harvestTime = 5 * 1000;

        client.StartAction(token => {
            Thread.Sleep(harvestTime);
            if(token.IsCancellationRequested)
                return;

            lock(client.Player) {
                client.AddFromLootTable(resource.LootTable);
                client.AddExpAction(skill, resource.Level);
            }

            Send06_01(client, 7, 0);
        }, () => {
            Send06_01(client, 8, 0);
        });

        Send06_01(client, 2, harvestTime);
    }
    #endregion

    #region Response
    // 06_01
    static void Send06_01(Client client, byte type, int time) {
        var b = new PacketBuilder(0x06, 0x01);

        b.WriteByte(type);
        // 1  = "Item is being used"
        // 2  = start
        // 3  = "Cannot get resources right now"
        // 4  = "Your crafting/collection tool does not meet the level requirement: %s"
        // 5  = "You are not equipped with the right tools"
        // 6  = nothing?
        // 7  = end
        // 8  = "Action cancelled"
        // 9  = "You cannot use a Scrapped Tool to collect resources"
        // 10 = "You can't clean up the Pile of Litter because you didn't help shoo away the Litterbug"
        // 11 = "There are other litter piles to be cleaned up. Give others a chance to clean up as well"

        // if(prev == 4) itemId else harvestTime
        b.WriteInt(time);

        b.Send(client);
    }
    #endregion
}
