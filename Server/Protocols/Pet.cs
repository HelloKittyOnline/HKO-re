using System;

namespace Server.Protocols;

static class Pet {
    [Request(0x0D, 0x02)] // 00536928
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x03)] // 0053698a
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x05)] // 00536a60
    public static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x06)] // 00536ae8
    public static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x07)] // 00536b83
    public static void Recv07(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x09)] // 00536bea
    public static void Recv09(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0A)] // 00536c6c
    public static void Recv0A(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0B)] // 00536cce
    public static void Recv0B(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0C)] // 00536d53
    public static void Recv0C(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0D)] // 00536dc8  
    public static void Recv0D(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0E)] // 00536e6e
    public static void Recv0E(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x0F)] // 00536ee8
    public static void Recv0F(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x10)] // 00536f73
    public static void Recv10(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x11)] // 00536fe8
    public static void Recv11(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x12)] // 0053705c
    public static void Recv12(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x0D, 0x13)] // 005370be // get pet information?
    public static void Recv13(ref Req req, Client client) { throw new NotImplementedException(); }
}
