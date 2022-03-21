using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Trade {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x08_01: // trade invite
                // case 0x08_02: //
                // case 0x08_03: //
                // case 0x08_04: //
                // case 0x08_06: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 08_{id:X2}");
                    break;
            }
        }
    }
}