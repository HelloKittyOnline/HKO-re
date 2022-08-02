namespace Server.Protocols;

static class Farm {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            // case 0x01: // 00581350 // enter farm
            // case 0x02: // 005813c4 // cancel enter farm
            // case 0x03: // 0058148c
            // case 0x04: // 00581504 // get all farms?
            // case 0x05: // get friend farms
            // case 0x06: // get guild farms
            // case 0x07: //
            // case 0x08: //
            // case 0x09: //
            // case 0x0B: // open farm management
            // case 0x0C: //
            // case 0x0E: //
            // case 0x0F: //
            // case 0x16: //
            // case 0x18: //
            // case 0x19: //
            // case 0x1A: //
            // case 0x1B: //
            // case 0x1C: //
            // case 0x24: //
            // case 0x25: //
            default:
                client.LogUnknown(0x0A, id);
                break;
        }
    }
}
