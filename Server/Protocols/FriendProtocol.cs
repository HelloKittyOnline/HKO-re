using System;
using System.IO;
using System.Text;

namespace Server {
    class FriendProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 0051afb7 // add friend
                    AddFriend(req, res);
                    break;
                /*case 0x04_02: // 0051b056 // mail
                case 0x04_03: // 0051b15e // delete friend
                */
                case 0x04: // 0051b1d4 // set status message // 1 byte, 0 = avalible, 1 = busy, 2 = away
                    SetStatus(req, res);
                    break;
                case 0x05: // 0051b253 // add player to blacklist
                    AddBlacklist(req, res);
                    break;
                // case 0x04_07: // 0051b31c // remove player from blacklist

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 04_01
        static void AddFriend(BinaryReader req, Stream res) {
            var name = Encoding.Unicode.GetString(req.ReadBytes(req.ReadInt16()));
        }

        // 04_05
        static void SetStatus(BinaryReader req, Stream res) {
            var status = req.ReadByte();
            // 0 = online
            // 1 = busy
            // 2 = afk
        }
        #endregion

        #region Response
        // 04_05
        static void AddBlacklist(BinaryReader req, Stream res) {
            var name = Encoding.Unicode.GetString(req.ReadBytes(req.ReadInt16()));
        }
        #endregion
    }
}