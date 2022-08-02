namespace Server.Protocols;

static class PODHousing {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            default:
                client.LogUnknown(0x18, id);
                break;
        }
    }
}
