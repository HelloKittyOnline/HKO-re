namespace Server.Protocols;

static class Pet {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x0D_02: // 00536928
            // case 0x0D_03: // 0053698a
            // case 0x0D_05: // 00536a60
            // case 0x0D_06: // 00536ae8
            // case 0x0D_07: // 00536b83
            // case 0x0D_09: // 00536bea
            // case 0x0D_0A: // 00536c6c
            // case 0x0D_0B: // 00536cce
            // case 0x0D_0C: // 00536d53
            // case 0x0D_0D: // 00536dc8
            // case 0x0D_0E: // 00536e6e
            // case 0x0D_0F: // 00536ee8
            // case 0x0D_10: // 00536f73
            // case 0x0D_11: // 00536fe8
            // case 0x0D_12: // 0053705c
            // case 0x0D_13: // 005370be // get pet information?
            default:
                client.LogUnknown(0x0D, id);
                break;
        }
    }
}
