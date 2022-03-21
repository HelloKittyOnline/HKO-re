using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Store {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: // 0054d96c
                // case 0x02: //
                // case 0x03: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 0B_{id:X2}");
                    break;
            }
        }
    }
}