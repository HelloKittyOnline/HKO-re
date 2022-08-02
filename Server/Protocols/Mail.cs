namespace Server.Protocols;

static class Mail {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x23_01: // 005a19da
            default:
                client.LogUnknown(0x23, id);
                break;
        }
    }
}
