using System;

namespace Server.Protocols;

static class EarthDay {
    [Request(0x19, 0x01)] //
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x19, 0x02)] //
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x19, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
}
