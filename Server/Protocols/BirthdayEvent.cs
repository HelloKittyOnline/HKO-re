using System;

namespace Server.Protocols;

static class BirthdayEvent {
    [Request(0x22, 0x03)] //
    static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x22, 0x04)] //
    static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x22, 0x05)] //
    static void Recv05(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x22, 0x06)] //
    static void Recv06(ref Req req, Client client) { throw new NotImplementedException(); }
}
