using System;

namespace Server.Protocols;

static class Group {
    #region Request
    [Request(0x13, 0x01)] // 00578950 // add player to group
    static void AddToGroup(ref Req req, Client client) {
        var name = req.ReadWString();

        var group = req.ReadInt32(); // group id
        var playerId = req.ReadInt32(); // player id?
        // playerId = 0 -> unknown

        throw new NotImplementedException();
    }

    // [Request(0x13, 0x02)] //
    // [Request(0x13, 0x03)] //
    // [Request(0x13, 0x04)] //
    // [Request(0x13, 0x05)] //
    // [Request(0x13, 0x06)] //
    // [Request(0x13, 0x07)] //
    // [Request(0x13, 0x08)] //
    // [Request(0x13, 0x09)] //
    // [Request(0x13, 0x0A)] //
    // [Request(0x13, 0x0B)] //
    // [Request(0x13, 0x0C)] //
    // [Request(0x13, 0x0D)] //

    #endregion
}
