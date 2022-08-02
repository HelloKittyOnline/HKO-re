namespace Server.Protocols;

static class Friend {
    public static void Handle(Client client) {
        var id = client.ReadByte();
        switch(id) {
            case 0x01: // 0051afb7 // add friend
                AddFriend(client);
                break;
            /*case 0x04_02: // 0051b056 // mail
            case 0x04_03: // 0051b15e // delete friend
            */
            case 0x04: // 0051b1d4 // set status message // 1 byte, 0 = avalible, 1 = busy, 2 = away
                SetStatus(client);
                break;
            case 0x05: // 0051b253 // add player to blacklist
                AddBlacklist(client);
                break;
            // case 0x04_07: // 0051b31c // remove player from blacklist

            default:
                client.LogUnknown(0x04, id);
                break;
        }
    }

    #region Request
    // 04_01
    static void AddFriend(Client client) {
        var name = client.ReadWString();
    }

    // 04_04
    static void SetStatus(Client client) {
        var status = client.ReadByte();
        // 0 = online
        // 1 = busy
        // 2 = afk
    }
    // 04_05
    static void AddBlacklist(Client client) {
        var name = client.ReadWString();
    }
    #endregion

    #region Response
    #endregion
}
