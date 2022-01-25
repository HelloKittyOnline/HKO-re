using System;
using System.IO;
using System.Linq;

namespace Server {
    partial class Program {
        // 00_01
        public static void SendLobby(Stream clientStream, bool lobby) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer", 1);

            b.WriteShort(0); // (*global_hko_client)->field_0xec
            b.WriteShort(1); // (*global_hko_client)->playerId

            b.Send(clientStream);
        }

        // 00_02_01
        public static void SendAcceptClient(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x1); // third switch

            b.AddString("", 1);
            b.AddString("", 1); // appended to username??
            b.AddString("", 1); // blowfish encrypted stuff???

            b.Send(clientStream);
        }

        // 00_02_02
        public static void SendInvalidLogin(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x2); // third switch

            b.Send(clientStream);
        }

        // 00_02_03
        public static void SendPlayerBanned(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x3); // third switch

            b.AddString("01/01/1999", 1); // unban timeout (01/01/1999 = never)

            b.Send(clientStream);
        }

        // 00_04
        public static void SendServerList(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x4); // second switch

            // some condition?
            b.WriteShort(0);

            // server count
            b.WriteInt(1);
            {
                b.WriteInt(1); // server number
                b.AddWstring("Test Sevrer");

                // world count
                b.WriteInt(1);
                {
                    b.WriteInt(1); // wolrd number
                    b.AddWstring("Test World");
                    b.WriteInt(0); // world status
                }
            }


            b.Send(clientStream);
        }

        // 00_05
        public static void Send00_05(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x5); // second switch

            int count = 1;
            b.WriteInt(count);

            for(int i = 1; i <= count; i++) {
                b.WriteInt(i); // id??
                b.AddString("Test server", 1);
            }

            b.Send(clientStream);
        }

        // 00_0B
        public static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xB); // second switch

            b.WriteInt(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1", 1); // address
            b.WriteShort(12345); // port

            b.Send(clientStream);
        }

        // 00_0C_x // x = 0-7
        public static void Send00_0C(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xC); // second switch

            b.WriteByte(x); // 0-7 switch

            b.Send(clientStream);
        }

        // 00_0D_x // x = 2-6
        public static void Send00_0D(Stream clientStream, short x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xD); // second switch

            b.WriteShort(x); // (2-6) switch

            b.Send(clientStream);
        }

        // 00_0E
        // almost the same as 00_0B
        public static void Send00_0E(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xE); // second switch

            b.WriteInt(0); // some global

            // parameters for FUN_0060699c
            b.AddString("127.0.0.1", 1);
            b.WriteShort(12345);

            b.Send(clientStream);
        }

        // 00_11
        public static void Send00_11(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x00); // first switch
            b.WriteByte(0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.WriteInt(1 << 16);

            b.Send(clientStream);
        }

        public static void writeInvItem(PacketBuilder w) {
            w.WriteInt(0); // id
            w.WriteInt(0);
            w.WriteInt(0);
        }
        public static void writeFriend(PacketBuilder w) {
            // name - wchar[32]
            for(int i = 0; i < 32; i++)
                w.WriteShort(0);
            w.WriteInt(0); // length
        }
        public static void writePetData(PacketBuilder w) {
            for(int i = 0; i < 0xd8; i++)
                w.WriteByte(0);
        }

        // 01_02
        // triggers character creation
        public static void SendCharacterData(Stream clientStream, PlayerData player) {
            var b = new PacketBuilder();

            b.WriteByte(0x1); // first switch
            b.WriteByte(0x2); // second switch

            bool exists = player != null;

            // indicates if a character already exists
            b.WriteByte(Convert.ToByte(exists));

            if(exists) {
                b.AddWstring(player.Name); // Character name
                b.WriteByte(player.Gender); // gender (1 = male, else female)

                b.BeginCompress(); // ((*global_gameData)->data).ItemAttEntityIds

                for(int i = 0; i < 18; i++) {
                    b.WriteInt(player.DisplayEntities[i]);
                }

                b.WriteInt(player.Money); // money

                b.WriteByte(0); // status (0 = online, 1 = busy, 2 = away)
                b.WriteByte(0); // active petId
                b.WriteByte(0); // emotionSomething
                b.WriteByte(0); // unused
                b.WriteByte(player.BloodType); // blood type
                b.WriteByte(player.BirthMonth); // birth month
                b.WriteByte(player.BirthDay); // birth day
                b.WriteByte(1); // constellation // todo: calculate this from brithday

                b.WriteInt(0); // guild id?

                for(int i = 0; i < 10; i++)
                    b.WriteInt(0); // quick bar

                for(int i = 0; i < 76; i++)
                    b.WriteInt(0); // idk

                for(int i = 0; i < 14; i++)
                    writeInvItem(b); // inv1
                for(int i = 0; i < 6; i++)
                    writeInvItem(b); // inv2
                for(int i = 0; i < 50; i++)
                    writeInvItem(b); // inv3
                b.WriteByte(0); // inv3 size
                b.WriteByte(0); b.WriteByte(0); b.WriteByte(0);
                for(int i = 0; i < 200; i++)
                    writeInvItem(b); // inv4
                b.WriteByte(0); // inv4 size
                b.WriteByte(0); b.WriteByte(0); b.WriteByte(0);

                for(int i = 0; i < 100; i++)
                    writeFriend(b); // friend list
                b.WriteByte(0); // friend count
                b.WriteByte(0); b.WriteByte(0); b.WriteByte(0);

                for(int i = 0; i < 50; i++)
                    writeFriend(b); // ban list
                b.WriteByte(0); // ban count
                b.WriteByte(0); b.WriteByte(0); b.WriteByte(0);

                for(int i = 0; i < 3; i++)
                    writePetData(b); // pet data

                b.EndCompress();

                b.WriteShort(0); // ((*global_gameData)->data).field_0x5410

                // map id
                b.WriteInt(player.CurrentMap);

                b.BeginCompress(); // starts at 0x911C

                b.WriteInt(1); // overall level
                b.WriteInt(0); // level progress

                b.WriteByte(0); // ???
                b.WriteByte(0); // ???
                b.WriteByte(0); // ???
                b.WriteByte(0); // unused?

                b.WriteShort(1); // Planting
                b.WriteShort(1); // Mining
                b.WriteShort(1); // Woodcutting
                b.WriteShort(1); // Gathering
                b.WriteShort(1); // Forging
                b.WriteShort(1); // Carpentry
                b.WriteShort(1); // Cooking
                b.WriteShort(1); // Tailoring

                b.WriteInt(0); // Planting    progress
                b.WriteInt(0); // Mining      progress
                b.WriteInt(0); // Woodcutting progress
                b.WriteInt(0); // Gathering   progress
                b.WriteInt(0); // Forging     progress
                b.WriteInt(0); // Carpentry   progress
                b.WriteInt(0); // Cooking     progress
                b.WriteInt(0); // Tailoring   progress

                b.EndCompress();
            }

            b.Send(clientStream);
        }

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
            for(int i = 0; i < 32; i++) {
                b.WriteShort(0);
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
        public static void SendPlayerHpSta(Stream clientStream, PlayerData player) {
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x12); // second switch

            b.WriteShort((short)player.Id); // player id

            b.BeginCompress();
            b.WriteInt(player.Hp); // hp
            b.WriteInt(player.MaxHp); // hp max
            b.WriteInt(player.Sta); // sta
            b.WriteInt(player.MaxSta); // sta max
            b.EndCompress();

            b.Send(clientStream);
        }

        // 02_0F
        public static void SendTeleportPlayer(Stream clientStream, PlayerData player) {
            var b = new PacketBuilder();

            b.WriteByte(0x2); // first switch
            b.WriteByte(0xF); // second switch

            b.WriteShort((short)player.Id); // player id
            b.WriteInt(player.PositionX); // x
            b.WriteInt(player.PositionY); // y

            b.Send(clientStream);
        }

        public static void writeTeleport(PacketBuilder w, Extractor.T_Teleport tp) {
            w.WriteInt(tp.Id); // id
            w.WriteInt(tp.fromX); // x
            w.WriteInt(tp.fromY); // y
            w.WriteInt(0); // flagId
            w.WriteByte((byte)tp.rotation); // direction
            w.WriteByte(0); w.WriteByte(0); w.WriteByte(0); // unused
            w.WriteInt(0); // somethingTutorial
            w.WriteInt(0); // roomNum
            w.WriteInt(0); // consumeItem
            w.WriteInt(0); // consumeItemCount
            w.WriteByte(0); // byte idk
            w.WriteByte(0); // unused
            w.WriteShort(0); // stringId
            w.WriteInt(0); // keyItem
        }
        public static void SendTeleporters(Stream clientStream, int mapId) {
            // 02_14 and 02_15
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x14); // second switch

            var valid = teleporters.Where(x => x.fromMap == mapId).ToArray();

            b.WriteInt(valid.Length); // count

            b.BeginCompress();
            foreach(var teleporter in valid) {
                writeTeleport(b, teleporter);
            }
            b.EndCompress();

            b.Send(clientStream);
        }

        public static void writeNpcData(PacketBuilder w, Extractor.T_NPCName npc) {
            w.WriteInt(npc.Id); // entity/npc id
            w.WriteInt(npc.x); // x 
            w.WriteInt(npc.y); // y

            w.WriteByte((byte)npc.r); // rotation
            w.WriteByte(0); w.WriteByte(0); w.WriteByte(0); // unused

            w.WriteInt(0);
            w.WriteInt(0);
            w.WriteInt(0);
            w.WriteInt(0);
        }
        // 02_16
        public static void SendNpcs(Stream clientStream, int mapId) {
            // create npcs
            var b = new PacketBuilder();

            b.WriteByte(0x02); // first switch
            b.WriteByte(0x16); // second switch

            var valid = npcs.Where(x => x.map == mapId).ToArray();

            b.WriteInt(valid.Length); // count

            b.BeginCompress();
            foreach(var npc in valid) {
                writeNpcData(b, npc);
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
    }
}
