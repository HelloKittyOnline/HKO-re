using System;
using System.IO;
using System.Text;

namespace Server {
    partial class Program {
        enum Maps {
            Dream_Room_1 = 1,
            Dream_Room_2 = 2,
            Dream_Room_3 = 3,
            Dream_Room_4 = 4,
            Dream_Room_5 = 5,
            Dream_Room_6 = 6,
            Dream_Room_7 = 7,
            Sanrio_Harbour = 8, // p2
            Florapolis = 9, // p3
            London = 10, // p7
            Paris = 11, // p5
            New_York = 12, // p4
            Tokyo = 14, // p9
            Dream_Carnival = 15,
            O2 = 16,
            Big_Ben = 17,
            Buckingham_Palace = 18,
            Chatting_Room = 25,
            West_Stars_Plain = 50, // p5
            Forbidden_Museum = 75, // p6
            Mt_Fujiyama = 100,
            TEST_South_Dream_Forest = 300 // p3
        }
        static int MapId = (int)Maps.Sanrio_Harbour;
        static short startX = 7730;
        static short startY = 6040;

        static void AcceptClient(BinaryReader req, Stream res) {
            var data = PacketBuilder.DecodeCrazy(req);

            var userName = Encoding.ASCII.GetString(data, 1, data[0]);
            var password = Encoding.ASCII.GetString(data, 0x42, data[0x41]);

            Console.WriteLine($"{userName}, {password}");

            SendAcceptClient(res);
        }

        static void ServerList(BinaryReader req, Stream res) {
            var count = req.ReadInt32();

            for(int i = 0; i < count; i++) {
                var len = req.ReadByte();
                var name = Encoding.ASCII.GetString(req.ReadBytes(len));
            }

            SendServerList(res);
        }

        static void SelectServer(BinaryReader req, Stream res) {
            int serverNum = req.ReadInt16();
            int worldNum = req.ReadInt16();

            // SendChangeServer(res);
            SendLobby(res, false);
        }

        static void Ping(BinaryReader req, Stream res) {
            int number = req.ReadInt32();
            // Console.WriteLine($"Ping {number}");
        }

        static void Recieve_00_0B(BinaryReader req, Stream res) {
            var idk1 = Encoding.ASCII.GetString(req.ReadBytes(req.ReadByte())); // "@"
            var idk2 = req.ReadInt32(); // = 0

            Send00_0C(res, 1);
            // SendCharacterData(res, false);
        }

        static void CreateCharacter(BinaryReader req, Stream res) {
            req.ReadByte(); // 0

            var data = PacketBuilder.DecodeCrazy(req);
            // data.length == 124

            var name = Encoding.Unicode.GetString(data[..64]);
            // cut of null terminated
            name = name[..name.IndexOf((char)0)];
            Console.WriteLine($"{name}");

            var gender = data[64]; // 1 = male, 2 = female
            var bloodType = data[65]; // 1 = O, 2 = A, 3 = B, 4 = AB
            var birthMonth = data[66];
            var birthDay = data[67];
            var idk5 = new int[14];

            // idk5[0] = skin id 
            // idk5[2] = eye id
            // idk5[6] = hair id
            Buffer.BlockCopy(data, 68, idk5, 0, 14 * 4);
            for(int i = 0; i < 14; i++) {
                Console.WriteLine(idk5[i]);
            }

            SendCharacterData(res, true);
        }

        static void Recieve_02_32(BinaryReader req, Stream res) {
            int count = req.ReadInt32();
            for(int i = 0; i < count; i++) {
                int aLen = req.ReadByte();
                var name = Encoding.ASCII.GetString(req.ReadBytes(aLen));

                int bLen = req.ReadByte();
                var version = Encoding.ASCII.GetString(req.ReadBytes(bLen));

                // Console.WriteLine($"{name} : {version}");
            }

            // Send02_6E(clientStream);
        }
    }
}
