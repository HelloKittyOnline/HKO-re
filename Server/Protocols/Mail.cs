using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Mail {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x23_01: // 005a19da
                default:
                    client.Logger.LogWarning($"Unknown Packet 23_{id:X2}");
                    break;
            }
        }
    }
}