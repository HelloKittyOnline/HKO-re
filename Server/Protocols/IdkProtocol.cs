using System;
using System.IO;
using System.Text;

namespace Server {
    class IdkProtocol {
        public static void Handle(BinaryReader req, Stream res, Account account) {
            switch(req.ReadByte()) {
                case 0x01: // 00566b0d // sent after character creation
                    CreateCharacter(req, res, account);
                    break;
                case 0x02: // 00566b72
                    GetCharacter(req, res, account.PlayerData);
                    break;
                case 0x03: // 00566bce // Delete character
                    account.PlayerData = null;
                    SendCharacterData(res, null);
                    break;
                case 0x05: // 00566c47 // check character name
                    CheckName(req, res);
                    break;
                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 01_01
        static void CreateCharacter(BinaryReader req, Stream res, Account account) {
            req.ReadByte(); // 0

            var data = PacketBuilder.DecodeCrazy(req);
            // data.length == 124

            var name = Encoding.Unicode.GetString(data[..64]);
            // cut of null terminated
            name = name[..name.IndexOf((char)0)];

            var entities = new int[18];
            Buffer.BlockCopy(data, 68, entities, 0, 14 * 4);
            for(int i = 0; i < 14; i++) {
                Console.WriteLine(entities[i]);
            }

            account.PlayerData = new PlayerData(
                name,
                data[64], // 1 = male, 2 = female
                data[65], // 1 = O, 2 = A, 3 = B, 4 = AB
                data[66], // birthMonth
                data[67], // birthDay
                entities);

            SendCharacterData(res, account.PlayerData);
        }

        // 01_02
        static void GetCharacter(BinaryReader req, Stream res, PlayerData player) {
            SendCharacterData(res, player);
        }
         
        // 01_03
        static void CheckName(BinaryReader req, Stream res) {
            var len = req.ReadInt16();
            var name = Encoding.Unicode.GetString(req.ReadBytes(len));

            // TODO: check with database
            // if(Program.database.CharacterExists(name)) { }
        }
        #endregion

        static void writeFriend(PacketBuilder w) {
            // name - wchar[32]
            for(int i = 0; i < 32; i++)
                w.WriteShort(0);
            w.WriteInt(0); // length
        }
        static void writePetData(PacketBuilder w) {
            for(int i = 0; i < 0xd8; i++)
                w.WriteByte(0);
        }

        #region Response
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

                b.Write0(76); // idk

                var empty = new InventoryItem();
                for(int i = 0; i < 14; i++) empty.Write(b); // inv1
                for(int i = 0; i < 6; i++) empty.Write(b); // inv2
                for(int i = 0; i < 50; i++) player.Inventory[i].Write(b); // inv3
                b.WriteByte((byte)player.InventorySize); // inv3 size
                b.Write0(3); // unused
                for(int i = 0; i < 200; i++) empty.Write(b); // inv4
                b.WriteByte(0); // inv4 size
                b.Write0(3); // unused

                for(int i = 0; i < 100; i++) writeFriend(b); // friend list
                b.WriteByte(0); // friend count
                b.Write0(3); // unused

                for(int i = 0; i < 50; i++) writeFriend(b); // ban list
                b.WriteByte(0); // ban count
                b.Write0(3); // unused

                for(int i = 0; i < 3; i++) writePetData(b); // pet data

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
        #endregion
    }
}