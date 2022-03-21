using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class CityCookOff {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: //
                // case 0x02: //
                // case 0x03: //
                // case 0x04: //
                // case 0x06: //
                // case 0x07: //
                // case 0x0B: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 1F_{id:X2}");
                    break;
            }
        }
    }
}