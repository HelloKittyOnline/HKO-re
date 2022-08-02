namespace Server.Protocols;

static class Redeem {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x01: //
            default:
                client.LogUnknown(0x14, id);
                break;
        }
    }
}
