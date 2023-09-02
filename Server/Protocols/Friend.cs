using System;

namespace Server.Protocols;

static class Friend {
    #region Request
    [Request(0x04, 0x01)] // 0051afb7 // add friend
    static void AddFriend(ref Req req, Client client) {
        var name = req.ReadWString();

        throw new NotImplementedException();
    }

    // 0x04_02: // 0051b056 // mail
    // 0x04_03: // 0051b15e // delete friend

    // 04_04
    [Request(0x04, 0x04)] // 0051b1d4
    static void SetStatus(ref Req req, Client client) {
        var status = req.ReadByte();
        // 0 = online
        // 1 = busy
        // 2 = afk

        throw new NotImplementedException();
    }

    [Request(0x04, 0x05)] // 0051b253 // add player to blacklist
    static void AddBlacklist(ref Req req, Client client) {
        var name = req.ReadWString();

        throw new NotImplementedException();
    }

    // 0x04_07: // 0051b31c // remove player from blacklist
    #endregion

    #region Response
    #endregion
}
