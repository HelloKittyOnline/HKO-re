using System;

namespace Server.Protocols;

static class PODLeaderboard {
    [Request(0x17, 0x01)] // 0053a183
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x17, 0x02)] //
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x17, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x17, 0x04)] //
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x17, 0x05)] //
    public static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x17, 0x06)] //
    public static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
}
