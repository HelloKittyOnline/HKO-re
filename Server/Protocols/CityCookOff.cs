using System;

namespace Server.Protocols;

static class CityCookOff {
    [Request(0x1F, 0x01)] //
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x02)] //
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x04)] //
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x06)] //
    public static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x07)] //
    public static void Recv07(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1F, 0x0B)] //
    public static void Recv0B(ref Req req, Client client) { throw new NotImplementedException(); }
}
