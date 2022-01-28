using System;
using System.IO;
using System.Text;

namespace Server {
    class GroupProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 00578950 // add player to group
                    Recieve_13_01(req, res);
                    break;
                /*case 0x13_02: //
                case 0x13_03: //
                case 0x13_04: //
                case 0x13_05: //
                case 0x13_06: //
                case 0x13_07: //
                case 0x13_08: //
                case 0x13_09: //
                case 0x13_0A: //
                case 0x13_0B: //
                case 0x13_0C: //
                case 0x13_0D: //
                */
                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 13_01
        static void Recieve_13_01(BinaryReader req, Stream res) {
            var name = Encoding.Unicode.GetString(req.ReadBytes(req.ReadInt16()));

            var group = req.ReadInt32(); // group id
            var playerId = req.ReadInt32(); // player id?
            // playerId = 0 -> unknown
        }
        #endregion
    }
}
