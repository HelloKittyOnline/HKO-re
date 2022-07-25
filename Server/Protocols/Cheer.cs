namespace Server.Protocols {
    static class Cheer {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x03: // 00538ce8
                default:
                    client.LogUnknown(0x21, id);
                    break;
            }
        }
    }
}