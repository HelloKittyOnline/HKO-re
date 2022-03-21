using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class EarthDay {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: //
                // case 0x02: //
                // case 0x03: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 19_{id:X2}");
                    break;
            }
        }
    }
}