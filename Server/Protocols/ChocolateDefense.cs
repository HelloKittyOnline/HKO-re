using System;

namespace Server.Protocols;

static class ChocolateDefense {
    [Request(0x20, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x20, 0x04)] //
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
}
