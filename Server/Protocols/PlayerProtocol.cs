using System;
using System.IO;
using System.Text;
using Extractor;

namespace Server {
    class PlayerProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 005defa2
                    Recieve_02_01(req, res, account);
                    break;
                case 0x02: // 005df036 // sent after map load
                    break;
                case 0x04: // 005df0cb // player walking
                    Recieve_02_04(req, res, account);
                    break;
                case 0x05: // 005df144 // open web form // maybe html request?
                    Recieve_02_05(req, res);
                    break;
                case 0x06: // 005df1ca // emotes
                    Recieve_02_06(req, res);
                    break;
                case 0x07: // 005df240 // player rotation changed
                    Recieve_02_07(req, res);
                    break;
                case 0x08: // 005df2b4 // player state (sitting/standing)
                    Recieve_02_08(req, res);
                    break;
                case 0x0A: // 005df368 // teleport map
                    ChangeMap(req, res, account.PlayerData);
                    break;
                case 0x0B: // 005df415
                    Recieve_02_0B(req, res);
                    break;
                /*
                case 0x02_0C: // 005df48c
                case 0x02_0D: // 005df50c
                case 0x02_0E: // 005df580
                case 0x02_13: // 005df5e2
                */
                case 0x1A: // 005df655 // sent after 02_09
                    Recieve_02_1A(req, res);
                    break;
                // case 0x02_1f: // 005df6e3
                case 0x20: // 005df763 // change player info
                    Recieve_02_20(req, res);
                    break;
                /* case 0x02_21: // 005df7d8
                case 0x02_28: // 005df86e
                case 0x02_29: // 005df8e4
                case 0x02_2A: // 005df946
                case 0x02_2B: // 005df9cb
                case 0x02_2C: // 005dfa40
                case 0x02_2D: // 005dfab4
                */
                case 0x32: // 005dfb8c //  client version information
                    Recieve_02_32(req, res);
                    break;
                /*
                case 0x02_33: // 005dfc04
                case 0x02_34: // 005dfc78
                case 0x02_63: // 005dfcee*/

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 02_01
        static void Recieve_02_01(BinaryReader req, Stream res, Account account) {
            var data = req.ReadByte(); // idk
            var player = account.PlayerData;

            LoginProtocol.Send00_11(res);
            Send02_01(res, player);
            SendPlayerHpSta(res, account);

            var map = Program.maps[player.CurrentMap - 1];

            SendChangeMap(res, player);
            SendNpcs(res, map.Npcs);
            SendTeleporters(res, map.Teleporters);
            SendRes(res, map.Resources);
        }

        // 02_04
        static void Recieve_02_04(BinaryReader req, Stream res, Account account) {
            // player walking
            var mapId = req.ReadInt32(); // mapId
            var x = req.ReadInt32(); // x
            var y = req.ReadInt32(); // y

            var player = account.PlayerData;

            player.cancelSource?.Cancel();
            player.cancelSource = null;

            player.PositionX = x;
            player.PositionY = y;
        }

        // 02_05
        static void Recieve_02_05(BinaryReader req, Stream res) {
            var data = req.ReadByte();
            // 0 = close
        }

        // 02_06
        static void Recieve_02_06(BinaryReader req, Stream res) {
            var emote = req.ReadInt32();
            // 1 = blink
            // 2 = yay
            // ...
            // 26 = wave
        }

        // 02_07
        static void Recieve_02_07(BinaryReader req, Stream res) {
            var rotation = req.ReadInt16();
            // 1 = north
            // 2 = north east
            // 3 = east
            // 4 = south east
            // 5 = south
            // 6 = south west
            // 7 = west
            // 8 = north west
        }

        // 02_08
        static void Recieve_02_08(BinaryReader req, Stream res) {
            var state = req.ReadInt16();
            // 1 = standing
            // 3 = sitting
            // 4 = gathering
        }

        // 02_0A
        static void ChangeMap(BinaryReader req, Stream res, PlayerData player) {
            var tpId = req.ReadInt16();
            var idk = req.ReadByte(); // always 1?

            // todo: index check?
            var tp = Program.teleporters[tpId - 1];

            player.CurrentMap = tp.toMap;
            player.PositionX = tp.toX;
            player.PositionY = tp.toY;

            var map = Program.maps[tp.toMap - 1];

            SendChangeMap(res, player);
            SendNpcs(res, map.Npcs);
            SendTeleporters(res, map.Teleporters);
            SendRes(res, map.Resources);
        }

        // 02_0B
        static void Recieve_02_0B(BinaryReader req, Stream res) {
            var mapId = req.ReadInt32();
            var hashHex = req.ReadBytes(32);
        }

        // 02_1A
        static void Recieve_02_1A(BinaryReader req, Stream res) {
            var winmTime = req.ReadInt32();
        }

        // 02_20
        static void Recieve_02_20(BinaryReader req, Stream res) {
            var data = PacketBuilder.DecodeCrazy(req); // 970 bytes

            // TODO: null trim
            var birth    = Encoding.ASCII.GetString(data, 1, data[0]); // 0 - 37
            var phone    = Encoding.ASCII.GetString(data, 39, data[38]); // 38 - 63
            var location = Encoding.Unicode.GetString(data, 64, 36 * 2); // 63 - 135
            var email    = Encoding.ASCII.GetString(data, 137, data[136]); // 136 - 201
            var favorite = Encoding.Unicode.GetString(data, 202, 64 * 2); // 202 - 329
            var hobby    = Encoding.Unicode.GetString(data, 330, 160 * 2); // 330 - 649
            var intro    = Encoding.Unicode.GetString(data, 650, 160 * 2); // 650 - 969
        }

        // 02_32
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
        #endregion

        #region Response
        // 02_01
        public static void Send02_01(Stream clientStream, PlayerData player) {
            var b = new PacketBuilder();

            b.WriteByte(0x2); // first switch
            b.WriteByte(0x1); // second switch

            b.BeginCompress(); // 0x5384
            b.WriteInt(0); // server token?
            b.WriteByte(0); // char str length
            for(int i = 0; i < 65; i++) {
                b.WriteByte(0);
            }
            // null terminated wchar string
            { // player name
                var bytes = Encoding.Unicode.GetBytes(player.Name);
                b.Write(bytes);
                b.Write0(64 - bytes.Length);
            }

            // maybe this is not intended and i'm writing out of bounds here. can't tell

            for(int i = 0; i < 18; i++) {
                b.WriteByte(0); // idk
            }

            b.WriteInt(player.CurrentMap); // mapId
            b.WriteInt(player.PositionX); // x
            b.WriteInt(player.PositionY); // y

            b.WriteByte(0);
            b.WriteByte(0);
            b.WriteByte(0);
            b.WriteByte(player.Gender); // gender
            b.EndCompress();

            b.Send(clientStream);
        }

        // 02_02
        public static void Send02_02(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x2); // first switch
            b.WriteByte(0x2); // second switch

            b.WriteShort(0); // count
            b.EncodeCrazy(Array.Empty<byte>()); // count * 267 bytes

            b.Send(clientStream);
        }

        // 02_09
        public static void SendChangeMap(Stream clientStream, PlayerData player) {
            var b = new PacketBuilder();

            b.WriteByte(0x2); // first switch
            b.WriteByte(0x9); // second switch

            b.WriteInt(player.CurrentMap);
            b.WriteShort((short)player.PositionX);
            b.WriteShort((short)player.PositionY);
            b.WriteByte(0);

            /*if(mapType == 3) {
                b.EncodeCrazy(Array.Empty<byte>());
                b.Add((int)0);
                b.AddString("", 1);
                b.Add((byte)0);
                b.Add((byte)0);
                b.EncodeCrazy(Array.Empty<byte>());
                b.Add((int)0);
            } else if(mapType == 4) {
                b.EncodeCrazy(Array.Empty<byte>());
                b.EncodeCrazy(Array.Empty<byte>());
            }*/

            b.WriteByte(0);
            /*
            if(byte == 99) {
                // have_data
                b.Add((int)0);
                b.AddString("", 2);
            } else {
                // no_data
            }
            */

            b.Send(clientStream);
        }

        // 02_12
        public static void SendPlayerHpSta(Stream clientStream, Account account) {
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x12); // second switch

            b.WriteShort((short)account.PlayerId); // player id

            var player = account.PlayerData;

            b.BeginCompress();
            b.WriteInt(player.Hp); // hp
            b.WriteInt(player.MaxHp); // hp max
            b.WriteInt(player.Sta); // sta
            b.WriteInt(player.MaxSta); // sta max
            b.EndCompress();

            b.Send(clientStream);
        }

        // 02_0F
        public static void SendTeleportPlayer(Stream clientStream, Account account) {
            var b = new PacketBuilder();

            b.WriteByte(0x2); // first switch
            b.WriteByte(0xF); // second switch
            
            var player = account.PlayerData;

            b.WriteShort((short)account.PlayerId); // player id
            b.WriteInt(player.PositionX); // x
            b.WriteInt(player.PositionY); // y

            b.Send(clientStream);
        }

        public static void writeTeleport(PacketBuilder w, Extractor.Teleport tp) {
            w.WriteInt(tp.Id); // id
            w.WriteInt(tp.fromX); // x
            w.WriteInt(tp.fromY); // y
            w.WriteInt(0); // flagId
            w.WriteByte((byte)tp.rotation); // direction
            w.WriteByte(0);
            w.WriteByte(0);
            w.WriteByte(0); // unused
            w.WriteInt(0); // somethingTutorial
            w.WriteInt(0); // roomNum
            w.WriteInt(0); // consumeItem
            w.WriteInt(0); // consumeItemCount
            w.WriteByte(0); // byte idk
            w.WriteByte(0); // unused
            w.WriteShort(0); // stringId
            w.WriteInt(0); // keyItem
        }
        public static void SendTeleporters(Stream clientStream, Teleport[] teleporters) {
            // 02_14 and 02_15
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x14); // second switch

            b.WriteInt(teleporters.Length); // count

            b.BeginCompress();
            foreach(var teleporter in teleporters) {
                writeTeleport(b, teleporter);
            }
            b.EndCompress();

            b.Send(clientStream);
        }

        public static void writeNpcData(PacketBuilder w, NPCName npc) {
            w.WriteInt(npc.Id); // entity/npc id
            w.WriteInt(npc.x); // x 
            w.WriteInt(npc.y); // y

            w.WriteByte((byte)npc.r); // rotation
            w.Write0(3); // unused

            w.WriteInt(0);
            w.WriteInt(0);
            w.WriteInt(0);
            w.WriteInt(0);
        }
        // 02_16
        public static void SendNpcs(Stream clientStream, NPCName[] npcs) {
            // create npcs
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x16); // second switch

            b.WriteInt(npcs.Length); // count

            b.BeginCompress();
            foreach(var npc in npcs) {
                writeNpcData(b, npc);
            }
            b.EndCompress();

            b.Send(clientStream);
        }

        public static void writeResData(PacketBuilder w, Extractor.Resource res) {
            w.WriteInt(res.Id); // entity/npc id
            w.WriteInt(res.X); // x 
            w.WriteInt(res.Y); // y

            w.WriteShort(res.NameId); // nameId
            w.WriteShort(res.Count); // count

            w.WriteByte(1); // rotation
            w.Write0(3); // unused

            w.WriteShort(res.Type1); // type 1 - 0 = gather, 1 = mine, 2 = attack, 3 = ?
            w.WriteShort(res.Type2); // type 2 - 0 = gather, 1 = mine, 2 = attack

            w.WriteByte(0); // 5 = no lan man?
            w.Write0(3); // unused
        }
        // 02_17
        public static void SendRes(Stream clientStream, Resource[] resources) {
            // create npcs
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x17); // second switch

            b.WriteInt(resources.Length); // count

            b.BeginCompress();
            foreach(var res in resources) {
                writeResData(b, res);
            }
            b.EndCompress();

            b.Send(clientStream);
        }

        // 02_6E
        public static void Send02_6E(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x6E); // second switch

            b.AddWstring("");
            b.WriteInt(8); // map id?
            b.AddString("", 1);

            b.Send(clientStream);
        }

        // 02_6F
        public static void Send02_6F(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x6E); // second switch

            b.WriteByte(0);

            b.Send(clientStream);
        }

        #endregion
    }
}