using System;

namespace Server.Protocols;

static class NPCWalk {
    [Request(0x11, 0x01)] // 0059b6b4
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
}
