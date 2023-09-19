using System;

namespace Server.Protocols;

static class Trade {
    [Request(0x08, 0x01)] // trade invite
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x08, 0x02)] //
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x08, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x08, 0x04)] //
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x08, 0x06)] //
    public static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
}
