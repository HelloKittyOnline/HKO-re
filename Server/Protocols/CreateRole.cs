using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Server.Protocols {
    static class CreateRole {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                case 0x01: // 00566b0d // sent after character creation
                    CreateCharacter(client);
                    break;
                case 0x02: // 00566b72
                    GetCharacter(client);
                    break;
                case 0x03: // 00566bce // Delete character
                    client.Player = null;
                    SendCharacterData(client);
                    break;
                case 0x05: // 00566c47 // check character name
                    CheckName(client);
                    break;
                default:
                    client.Logger.LogWarning($"Unknown Packet 01_{id:X2}");
                    break;
            }
        }

        #region Request
        // 01_01
        static void CreateCharacter(Client client) {
            client.ReadByte(); // 0

            var data = PacketBuilder.DecodeCrazy(client.Reader);
            // data.length == 124

            var name = Encoding.Unicode.GetString(data[..64]);
            // cut of null terminated
            name = name[..name.IndexOf((char)0)];

            var entities = new int[18];
            Buffer.BlockCopy(data, 68, entities, 0, 14 * 4);

            client.Player = new PlayerData(
                name,
                data[64], // 1 = male, 2 = female
                data[65], // 1 = O, 2 = A, 3 = B, 4 = AB
                data[66], // birthMonth
                data[67], // birthDay
                entities);

            SendCharacterData(client);
        }

        // 01_02
        static void GetCharacter(Client client) {
            SendCharacterData(client);
        }

        // 01_03
        static void CheckName(Client client) {
            var name = client.ReadWString();

            // TODO: check with database
            // if(Program.database.CharacterExists(name)) { }
        }
        #endregion

        #region Response
        // 01_02
        // triggers character creation
        public static void SendCharacterData(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x1); // first switch
            b.WriteByte(0x2); // second switch

            if(client.Player == null) {
                // indicates if a character already exists
                b.WriteByte(0);
            } else {
                b.WriteByte(1);

                b.WriteWString(client.Player.Name); // Character name
                b.WriteByte(client.Player.Gender); // gender (1 = male, else female)

                b.BeginCompress(); // send character skin
                client.Player.WriteEntities(b);
                b.EndCompress();

                b.WriteShort(0); // ((*global_gameData)->data).field_0x5410

                // map id
                b.WriteInt(client.Player.CurrentMap);

                b.BeginCompress();
                client.Player.WriteLevels(b);
                b.EndCompress();
            }

            b.Send(client);
        }
        #endregion
    }
}