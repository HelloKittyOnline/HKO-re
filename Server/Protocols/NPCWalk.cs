using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class NPCWalk {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x11_01: // 0059b6b4
                default:
                    client.Logger.LogWarning($"Unknown Packet 11_{id:X2}");
                    break;
            }
        }
    }
}