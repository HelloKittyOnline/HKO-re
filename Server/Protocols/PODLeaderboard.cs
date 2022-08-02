namespace Server.Protocols;

static class PODLeaderboard {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x01: // 0053a183
            // case 0x02: //
            // case 0x03: //
            // case 0x04: //
            // case 0x05: //
            // case 0x06: //
            default:
                client.LogUnknown(0x17, id);
                break;
        }
    }
}
