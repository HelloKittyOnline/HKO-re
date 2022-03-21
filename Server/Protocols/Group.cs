using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class Group {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01: // 00578950 // add player to group
                    AddToGroup(client);
                    break;
                /*case 0x13_02: //
                case 0x13_03: //
                case 0x13_04: //
                case 0x13_05: //
                case 0x13_06: //
                case 0x13_07: //
                case 0x13_08: //
                case 0x13_09: //
                case 0x13_0A: //
                case 0x13_0B: //
                case 0x13_0C: //
                case 0x13_0D: //
                */
                default:
                    client.Logger.LogWarning($"Unknown Packet 13_{id:X2}");
                    break;
            }
        }

        #region Request
        // 13_01
        static void AddToGroup(Client client) {
            var name = client.ReadWString();

            var group = client.ReadInt32(); // group id
            var playerId = client.ReadInt32(); // player id?
            // playerId = 0 -> unknown
        }
        #endregion
    }
}
