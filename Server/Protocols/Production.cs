using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Production {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x07_01: // 00529917
                // case 0x07_04: // 005299a6
                default:
                    client.Logger.LogWarning($"Unknown Packet 07_{id:X2}");
                    break;
            }
        }
    }
}