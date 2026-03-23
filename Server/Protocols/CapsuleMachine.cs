using System;

namespace Server.Protocols;

static class CapsuleMachine {
    #region Request

    [Request(0x1C, 0x01)] // 0059bf6c
    public static void Recv01(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1C, 0x02)] // 0059bfe0
    public static void Recv02(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1C, 0x03)] // 0059c054
    public static void Recv03(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1C, 0x04)] // 0059c0e0
    public static void Recv04(ref Req req, Client client) { throw new NotImplementedException(); }
    [Request(0x1C, 0x0A)] // 0059c16c
    public static void Recv0A(ref Req req, Client client) { throw new NotImplementedException(); }

    #endregion

    #region Response

    // 1C_01 StartUfoMachine
    // 1C_02 StartCapsuleMachine
    // 1C_03 winMsg
    // 1C_04 winMsg
    // 1C_0A leaderboard

    #endregion
}
