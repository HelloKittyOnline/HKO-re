namespace Server.Protocols {
    static class ChocolateDefense {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x03: //
                // case 0x04: //
                default:
                    client.LogUnknown(0x20, id);
                    break;
            }
        }
    }
}