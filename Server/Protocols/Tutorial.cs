using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Tutorial {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: //
                // case 0x02: // start tutorial?
                default:
                    client.Logger.LogWarning($"Unknown Packet 16_{id:X2}");
                    break;
            }
        }
    }
}