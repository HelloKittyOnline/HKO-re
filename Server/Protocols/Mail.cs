using System;

namespace Server.Protocols;

static class Mail {
    [Request(0x23, 0x01)] // 005a19da
    public static void Recv01(Client client) {
        throw new NotImplementedException();
    }
}
