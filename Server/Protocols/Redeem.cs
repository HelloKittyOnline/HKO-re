using System;

namespace Server.Protocols;

static class Redeem {
    [Request(0x14, 0x01)] //
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
}
