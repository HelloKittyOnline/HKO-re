namespace Server.Protocols;

static class Food4Friends {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x01: //
            // case 0x02: //
            // case 0x03: //
            // case 0x04: //
            default:
                client.LogUnknown(0x15, id);
                break;
        }
    }
}
