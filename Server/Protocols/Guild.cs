namespace Server.Protocols {
    static class Guild {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x0E_01: // 0054ddb4
                // case 0x0E_02: //
                // case 0x0E_03: //
                // case 0x0E_04: //
                // case 0x0E_05: //
                // case 0x0E_06: //
                // case 0x0E_07: //
                // case 0x0E_09: //
                // case 0x0E_0A: //
                // case 0x0E_0B: //
                // case 0x0E_14: //
                default:
                    client.LogUnknown(0x0E, id);
                    break;
            }
        }
    }
}