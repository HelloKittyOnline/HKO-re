namespace Server.Protocols {
    static class Flash {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x01: //
                // case 0x02: //
                // case 0x03: //
                // case 0x04: //
                // case 0x0A: //
                default:
                    client.LogUnknown(0x1C, id);
                    break;
            }
        }
    }
}