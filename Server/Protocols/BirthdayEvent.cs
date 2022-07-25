namespace Server.Protocols {
    static class BirthdayEvent {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                // case 0x03: //
                // case 0x04: //
                // case 0x05: //
                // case 0x06: //
                default:
                    client.LogUnknown(0x22, id);
                    break;
            }
        }
    }
}