namespace Server.Protocols;

static class Hompy {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x0F_02: // 00511e18
            // case 0x0F_03: // 00511e8c
            // case 0x0F_04: // 00511f75
            // case 0x0F_05: // 00512053
            // case 0x0F_06: // 005120e6
            // case 0x0F_07: // 00512176
            // case 0x0F_09: // 005121da
            // case 0x0F_0A: // 00512236
            // case 0x0F_0B: // 005122a4
            // case 0x0F_0C: // 0051239d
            // case 0x0F_0D: // 0051242c
            // case 0x0F_0E: // 005124f7
            default:
                client.LogUnknown(0x0F, id);
                break;
        }
    }
}
