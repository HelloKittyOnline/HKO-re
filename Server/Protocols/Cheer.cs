using System;

namespace Server.Protocols;

static class Cheer {
    [Request(0x21, 0x03)] // 00538ce8
    static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
}
