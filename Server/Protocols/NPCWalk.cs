namespace Server.Protocols;

static class NPCWalk {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x11_01: // 0059b6b4
            default:
                client.LogUnknown(0x11, id);
                break;
        }
    }
}
