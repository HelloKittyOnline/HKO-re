using System.Threading.Tasks;

namespace Server.Protocols;

static class Resource {
    #region Request
    [Request(0x06, 0x01)] // 005a2513
    static void HarvestResource(ref Req req, Client client) {
        // gathering

        var resId = req.ReadInt32();
        var action = req.ReadByte(); // 1 or 2
        if(action is not (1 or 2)) {
            return;
        }

        var resource = Program.resources[resId];

        var skill = resource.GetSkill(action);
        var level = client.Player.Levels[(int)skill];
        var toolLevel = client.Player.GetToolLevel(skill);

        if(level < resource.Level || toolLevel < resource.Level) {
            SendMessage(client, 4);
            return;
        }

        // TODO: harvest time??
        const int harvestTime = 5 * 1000;

        client.StartAction(async token => {
            await Task.Delay(harvestTime);
            if(token.IsCancellationRequested)
                return;

            lock(client.Player) {
                client.AddFromLootTable(action == 1 ? resource.LootTable1 : resource.LootTable2);
                client.AddExpAction(skill, resource.Level);
            }

            SendMessage(client, 7);
        }, () => {
            SendMessage(client, 8);
        });

        SendMessage(client, 2, harvestTime);
    }
    #endregion

    #region Response
    // 06_01
    static void SendMessage(Client client, byte type, int time = 0) {
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
