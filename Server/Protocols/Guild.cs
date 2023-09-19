using System;

namespace Server.Protocols;

static class Guild {
    [Request(0x0E, 0x01)] // 0054ddb4
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x02)] //
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x03)] //
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x04)] //
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x05)] //
    public static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x06)] //
    public static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x07)] //
    public static void Recv07(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x09)] //
    public static void Recv09(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x0A)] //
    public static void Recv0A(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x0B)] //
    public static void Recv0B(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0E, 0x14)] //
    public static void Recv14(ref Req req, Client client) { throw new NotImplementedException(); }
}
