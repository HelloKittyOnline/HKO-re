using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Redeem {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: //
                default:
                    client.Logger.LogWarning($"Unknown Packet 14_{id:X2}");
                    break;
            }
        }
    }
}