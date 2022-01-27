using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    partial class Program {
        // 00_01
        static Account AcceptClient(BinaryReader req, Stream res) {
            var data = PacketBuilder.DecodeCrazy(req);

            var userName = Encoding.ASCII.GetString(data, 1, data[0]);
            var password = Encoding.UTF7.GetString(data, 0x42, data[0x41]);

            var account = database.GetPlayer(userName, password);

            if(account == null) {
                SendInvalidLogin(res);
                return null;
            }
            SendAcceptClient(res);
            return account;
        }

        // 00_03
        static void SelectServer(BinaryReader req, Stream res) {
            int serverNum = req.ReadInt16();
            int worldNum = req.ReadInt16();

            // SendChangeServer(res);
            SendLobby(res, false);
        }

        // 00_04
        static void ServerList(BinaryReader req, Stream res) {
            var count = req.ReadInt32();

            for(int i = 0; i < count; i++) {
                var len = req.ReadByte();
                var name = Encoding.ASCII.GetString(req.ReadBytes(len));
            }

            SendServerList(res);
        }

        // 00_0B
        static void Recieve_00_0B(BinaryReader req, Stream res) {
            var idk1 = Encoding.ASCII.GetString(req.ReadBytes(req.ReadByte())); // "@"
            var idk2 = req.ReadInt32(); // = 0

            Send00_0C(res, 1);
            // SendCharacterData(res, false);
        }

        // 00_63
        static void Ping(BinaryReader req, Stream res) {
            int number = req.ReadInt32();
            // Console.WriteLine($"Ping {number}");
        }

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
        static void DeleteCharacter(BinaryReader req, Stream res) { }

        // 02_01
        static void Recieve_02_01(BinaryReader req, Stream res, PlayerData player) {
            var data = req.ReadByte(); // idk

            Send00_11(res);
            Send02_01(res, player);
            SendPlayerHpSta(res, player);

            SendChangeMap(res, player);
            SendNpcs(res, player.CurrentMap);
            SendTeleporters(res, player.CurrentMap);
            SendRes(res, player.CurrentMap);
        }

        // 02_04
        static void Recieve_02_04(BinaryReader req, Stream res, PlayerData player) {
            // player walking
            var mapId = req.ReadInt32(); // mapId
            var x = req.ReadInt32(); // x
            var y = req.ReadInt32(); // y

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

        static void ChangeMap(BinaryReader req, Stream res, PlayerData player) {
            var tpId = req.ReadInt16();
            var idk = req.ReadByte(); // always 1?

            var tp = teleporters.First(x => x.Id == tpId);

            player.CurrentMap = tp.toMap;
            player.PositionX = tp.toX;
            player.PositionY = tp.toY;

            SendChangeMap(res, player);
            SendNpcs(res, player.CurrentMap);
            SendTeleporters(res, player.CurrentMap);
            SendRes(res, player.CurrentMap);
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

            // TODO: ascii length prefixed/null trim
            var birth    = Encoding.ASCII  .GetString(data, 0  , 38);
            var phone    = Encoding.ASCII  .GetString(data, 38 , 26);
            var location = Encoding.Unicode.GetString(data, 64 , 36 * 2);
            var email    = Encoding.ASCII  .GetString(data, 136, 66);
            var favorite = Encoding.Unicode.GetString(data, 202, 64 * 2);
            var hobby    = Encoding.Unicode.GetString(data, 330, 160 * 2);
            var intro    = Encoding.Unicode.GetString(data, 650, 160 * 2);
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

        // 04_05
        static void AddBlacklist(BinaryReader req, Stream res) {
            var name = Encoding.Unicode.GetString(req.ReadBytes(req.ReadInt16()));
        }

        static void Recieve_05_01(BinaryReader req, Stream res) {
            var npcId = req.ReadInt32();
            var npc = npcs.First(x => x.Id == npcId);

            Send05_01(res);
        }

        // 06_01
        static void Recieve_06_01(BinaryReader req, Stream res, PlayerData player) {
            // gathering

            var resId = req.ReadInt32();
            var idk2 = req.ReadByte(); // 1 or 2

            var table = resources.First(x => x.Id == resId).LootTable;

            // TODO: harvest time??
            const int harvestTime = 5 * 1000;

            if(table != 0) {
                var source = new CancellationTokenSource();
                player.cancelSource = source;

                Task.Run(() => {
                    Thread.Sleep(harvestTime);
                    if(source.IsCancellationRequested)
                        return;

                    var item = lootTables[table - 1].GetRandom();
                    if(item != -1) {
                        var pos = player.AddItem(item);
                        if(pos == -1) {
                            // inventory full
                        } else {
                            SendGetItem(res, (byte)(pos + 1), player.Inventory[pos]);
                        }
                    }
                });
            }
            
            Send06_01(res, harvestTime);
        }

        // 09_01
        static void Recieve_09_01(BinaryReader req, Stream res, PlayerData player) {
            var idk1 = req.ReadByte();
            var fromPos = req.ReadByte() - 1;

            var idk2 = req.ReadByte();
            var destPos = req.ReadByte() - 1;

            var from = player.Inventory[fromPos];
            var to = player.Inventory[destPos];
            if (to.Id == 0 || (to.Id == from.Id && to.Count + from.Count < 99)) {
                player.Inventory[fromPos] = new InventoryItem();

                player.Inventory[destPos].Id = from.Id;
                player.Inventory[destPos].Count += from.Count;

                SendSetItem(res, (byte)(fromPos + 1), player.Inventory[fromPos]);
                SendSetItem(res, (byte)(destPos + 1), player.Inventory[destPos]);
            } else {
                // fail
            }
        }

        // 09_06
        static void SplitItem(BinaryReader req, Stream res, PlayerData player) {
            var pos = req.ReadByte() - 1;
            var count = req.ReadByte();

            for (int i = 0; i < player.InventorySize; i++) {
                if (player.Inventory[i].Id != 0) continue;
                
                player.Inventory[i].Id = player.Inventory[pos].Id;
                player.Inventory[i].Count = count;

                player.Inventory[pos].Count -= count;

                SendSetItem(res, (byte)(i + 1), player.Inventory[i]);
                SendSetItem(res, (byte)(pos + 1), player.Inventory[pos]);
                break;
            }
        }

        // 09_20
        static void Recieve_09_20(BinaryReader req, Stream res) {
            // Send09_20(res);
        }

        // 0D_13
        static void Recieve_0D_13(BinaryReader req, Stream res) { }

        // 13_01
        static void Recieve_13_01(BinaryReader req, Stream res) {
            var name = Encoding.Unicode.GetString(req.ReadBytes(req.ReadInt16()));

            var group = req.ReadInt32(); // group id
            var playerId = req.ReadInt32(); // player id?
            // playerId = 0 -> unknown
        }
    }
}
