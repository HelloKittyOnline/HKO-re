using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class ChocolateDefense {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x03: //
                // case 0x04: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 20_{id:X2}");
                    break;
            }
        }
    }
}