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

    [Request(0x13, 0x02)] //
    static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x03)] //
    static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x04)] //
    static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x05)] //
    static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x06)] //
    static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x07)] //
    static void Recv07(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x08)] //
    static void Recv08(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x09)] //
    static void Recv09(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x0A)] //
    static void Recv0A(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x0B)] //
    static void Recv0B(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x0C)] //
    static void Recv0C(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x13, 0x0D)] //
    static void Recv0D(ref Req req, Client client) { throw new NotImplementedException(); }

    #endregion
}
