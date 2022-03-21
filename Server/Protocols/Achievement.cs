using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Achievement {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                default:
                    client.Logger.LogWarning($"Unknown Packet 1B_{id:X2}");
                    break;
            }
        }
    }
}