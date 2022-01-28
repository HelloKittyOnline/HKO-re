using System;
using System.IO;

namespace Server {
    class NpcProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 00573de8
                    Recieve_05_01(req, res);
                    break;
                case 0x02: // 00573e4a // npc data ack?
                    break;
                /*case 0x05_03: //
                case 0x05_04: //
                case 0x05_05: //
                case 0x05_06: //
                case 0x05_07: //
                case 0x05_08: //
                case 0x05_09: //
                case 0x05_0A: //
                case 0x05_0B: //
                case 0x05_0C: //
                case 0x05_11: //
                case 0x05_14: //
                case 0x05_15: //
                case 0x05_16: //
                */

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 05_01
        static void Recieve_05_01(BinaryReader req, Stream res) {
            var npcId = req.ReadInt32();
            // var npc = npcs.First(x => x.Id == npcId);

            Send05_01(res);
        }
        #endregion

        #region Response
        // 05_01
        public static void Send05_01(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(0); // dialog id (0 == npc default)

            b.Send(clientStream);
        }
        // 05_14
        public static void Send05_14(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x05); // first switch
            b.WriteByte(0x14); // second switch

            b.WriteByte(0x01);

            b.AddString("https://google.de", 1);

            b.Send(clientStream);
        }
        #endregion
    }
}