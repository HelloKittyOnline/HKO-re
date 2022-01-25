using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Extractor;

namespace Server {
    partial class Program {
        private static DataBase database;

        private static T_Teleport[] teleporters;
        private static T_NPCName[] npcs;

        static void listenClient(TcpClient socket, bool lobby) {
            Console.WriteLine($"Client {socket.Client.RemoteEndPoint} {socket.Client.LocalEndPoint}");

            var clientStream = socket.GetStream();
            var reader = new BinaryReader(clientStream);

            SendLobby(clientStream, lobby);

            Account account = null;

            bool running = true;
            while(running) {
                var head = reader.ReadBytes(3);
                if(head.Length == 0) {
                    break;
                }

                var data = reader.ReadBytes(reader.ReadUInt16());

                if(data.Length == 1) {
                    if(data[0] == 0x7E) { // 005551be
                    } else if(data[0] == 0x7F) { // 005bb8a9
                        // reset timeout
                        var b = new PacketBuilder();

                        b.WriteByte(0x7F);

                        b.Send(clientStream);
                    }
                    continue;
                }

                var ms = new MemoryStream(data);
                var r = new BinaryReader(ms);

                var id = (r.ReadByte() << 8) | r.ReadByte();

                if(account == null && id != 1) {
                    // invalid data
                    break;
                }

#if DEBUG
                if(id != 0x0063) { // skip ping
                    Console.WriteLine($"S <- C: {id >> 8:X2}_{id & 0xFF:X2}:");
                    // if(data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
                }
#endif

                // S -> C: 00_01  : SendLobby        // lobby server
                // S <- C: 00_01  : AcceptClient     // send login details
                // S -> C: 00_02_1: SendAcceptClient // check login details
                // S <- C: 00_04  : ServerList       // request server list (after License accept)
                // S -> C: 00_04  : SendServerList   // send server list
                // S <- C: 00_03  : SelectServer
                // (optional) S -> C: 00_0B: SendChangeServer // redirect to different server for load balancing
                // S -> C: 00_01  : SendLobby        // realm server?
                // S <- C: 00_0B  : Idk
                // (optional) S -> C: 01_02: create new character
                // (optional) S <- C: 01_01: CreateCharacter
                // S -> C: 00_0C_1: create T_EnterGame
                // S <- C: 01_02  : 
                // S -> C: 01_02  : SendCharacterData
                // S <- C: 02_32  :
                // short pause loading
                // S <- C: 02_01
                // S -> C: 00_11
                // S -> C: 02_01
                // S -> C: 02_09
                // S -> C: 02_12
                // S <- C: 00_10
                // S <- C: 02_32
                // S <- C: 02_02
                // S <- C: 02_1A
                // S <- C: 02_0B

                switch(id) {
                    case 0x00_01: // 0059af3e // Auth
                        account = AcceptClient(r, clientStream);
                        if(account == null) { // login failed
                            running = false;
                        }
                        break;
                    case 0x00_03: // 0059afd7 // after user selected world
                        SelectServer(r, clientStream);
                        break;
                    case 0x00_04: // 0059b08f // list of languages? sent after lobbyServer
                        ServerList(r, clientStream);
                        break;
                    case 0x00_0B: // 0059b14a // source location 0059b14a // sent after realmServer
                        Recieve_00_0B(r, clientStream);
                        break;
                    case 0x00_10: // 0059b1ae // has something to do with T_LOADScreen // finished loading?
                        break;
                    case 0x00_63: // 0059b253
                        Ping(r, clientStream);
                        break;

                    case 0x01_01: // 00566b0d // sent after character creation
                        CreateCharacter(r, clientStream, account);
                        break;
                    case 0x01_02: // 00566b72
                        GetCharacter(r, clientStream, account.PlayerData);
                        break;
                    case 0x01_03: // 00566bce // Delete character
                        DeleteCharacter(r, clientStream);
                        account.PlayerData = null;
                        SendCharacterData(clientStream, null);
                        break;
                    /*
                    case 0x01_05: // 00566c47 // check character name
                        // r.ReadInt16();
                        // rest wstring
                        break;*/

                    case 0x02_01: // 005defa2
                        Recieve_02_01(r, clientStream, account.PlayerData);
                        break;
                    case 0x02_02: // 005df036 // sent after map load
                        break;
                    case 0x02_04: // 005df0cb // player walking
                        Recieve_02_04(r, clientStream, account.PlayerData);
                        break;
                    case 0x02_05: // 005df144 // open web form // maybe html request?
                        Recieve_02_05(r, clientStream);
                        break;
                    case 0x02_06: // 005df1ca // emotes
                        Recieve_02_06(r, clientStream);
                        break;
                    case 0x02_07: // 005df240 // player rotation changed
                        Recieve_02_07(r, clientStream);
                        break;
                    case 0x02_08: // 005df2b4 // player state (sitting/standing)
                        Recieve_02_08(r, clientStream);
                        break;
                    case 0x02_0A: // 005df368 // teleport map
                        ChangeMap(r, clientStream, account.PlayerData);
                        break;
                    case 0x02_0B: // 005df415
                        Recieve_02_0B(r, clientStream);
                        break;
                    /*
                    case 0x02_0C: // 005df48c
                    case 0x02_0D: // 005df50c
                    case 0x02_0E: // 005df580
                    case 0x02_13: // 005df5e2
                    */
                    case 0x02_1A: // 005df655 // sent after 02_09
                        Recieve_02_1A(r, clientStream);
                        break;
                    // case 0x02_1f: // 005df6e3
                    case 0x02_20: // 005df763 // change player info
                        Recieve_02_20(r, clientStream);
                        break;
                    /* case 0x02_21: // 005df7d8
                    case 0x02_28: // 005df86e
                    case 0x02_29: // 005df8e4
                    case 0x02_2A: // 005df946
                    case 0x02_2B: // 005df9cb
                    case 0x02_2C: // 005dfa40
                    case 0x02_2D: // 005dfab4
                    */
                    case 0x02_32: // 005dfb8c //  client version information
                        Recieve_02_32(r, clientStream);
                        break;
                    /*
                    case 0x02_33: // 005dfc04
                    case 0x02_34: // 005dfc78
                    case 0x02_63: // 005dfcee
 
                    case 0x03_01: // 005d2eec // map channel message
                    case 0x03_02: // 005d2fa6
                    case 0x03_05: // 005d3044 // normal channel message
                    case 0x03_06: // 005d30dc // trade channel message
                    case 0x03_07: // 005d3174
                    case 0x03_08: // 005d320c // advice channel message
                    case 0x03_0B: // 005d3288 change chat filter
                    case 0x03_0C: // 005d331e
                    case 0x03_0D: // 005d33a7 open private message
                    */
                    case 0x04_01: // 0051afb7 // add friend
                        AddFriend(r, clientStream);
                        break;
                    /*case 0x04_02: // 0051b056 // mail
                    case 0x04_03: // 0051b15e // delete friend
                    */
                    case 0x04_04: // 0051b1d4 // set status message // 1 byte, 0 = avalible, 1 = busy, 2 = away
                        SetStatus(r, clientStream);
                        break;
                    case 0x04_05: // 0051b253 // add player to blacklist
                        AddBlacklist(r, clientStream);
                        break;
                    // case 0x04_07: // 0051b31c // remove player from blacklist

                    case 0x05_01: // 00573de8
                        Recieve_05_01(r, clientStream);
                        break;
                    case 0x05_02: // 00573e4a // npc data ack?
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

                    case 0x06_01: // 005a2513

                    case 0x07_01: // 00529917
                    case 0x07_04: // 005299a6

                    case 0x08_01: //
                    case 0x08_02: //
                    case 0x08_03: //
                    case 0x08_04: //
                    case 0x08_06: //

                    case 0x09_01: // 00586fd2
                    case 0x09_02: // 
                    case 0x09_03: // 
                    case 0x09_06: // 
                    case 0x09_0F: // 
                    case 0x09_10: // 
                    case 0x09_11: // 
                    */
                    case 0x09_20: // check item delivery available?
                        Recieve_09_20(r, clientStream);
                        break;
                    /*case 0x09_21: // 
                    case 0x09_22: // 

                    case 0x0A_01: // 00581350
                    case 0x0A_02: // 005813c4
                    case 0x0A_03: // 0058148c
                    case 0x0A_04: // 00581504
                    case 0x0A_05: // 
                    case 0x0A_06: // 
                    case 0x0A_07: // 
                    case 0x0A_08: // 
                    case 0x0A_09: // 
                    case 0x0A_0B: // 
                    case 0x0A_0C: // 
                    case 0x0A_0E: // 
                    case 0x0A_0F: // 
                    case 0x0A_16: // 
                    case 0x0A_18: // 
                    case 0x0A_19: // 
                    case 0x0A_1A: // 
                    case 0x0A_1B: // 
                    case 0x0A_1C: // 
                    case 0x0A_24: // 
                    case 0x0A_25: // 

                    case 0x0B_01: // 0054d96c
                    case 0x0B_02: // 
                    case 0x0B_03: // 

                    case 0x0C_03: // 00537da8
                    case 0x0C_07: // 00537e23
                    case 0x0C_08: // 00537e98
                    case 0x0C_09: // 00537f23

                    case 0x0D_02: // 00536928
                    case 0x0D_03: // 0053698a
                    case 0x0D_05: // 00536a60
                    case 0x0D_06: // 00536ae8
                    case 0x0D_07: // 00536b83
                    case 0x0D_09: // 00536bea
                    case 0x0D_0A: // 00536c6c
                    case 0x0D_0B: // 00536cce
                    case 0x0D_0C: // 00536d53
                    case 0x0D_0D: // 00536dc8
                    case 0x0D_0E: // 00536e6e
                    case 0x0D_0F: // 00536ee8
                    case 0x0D_10: // 00536f73
                    case 0x0D_11: // 00536fe8
                    case 0x0D_12: // 0053705c
                    */
                    case 0x0D_13: // 005370be // get pet information?
                        Recieve_0D_13(r, clientStream);
                        break;
                    /*case 0x0E_01: // 0054ddb4
                    case 0x0E_02: //
                    case 0x0E_03: //
                    case 0x0E_04: //
                    case 0x0E_05: //
                    case 0x0E_06: //
                    case 0x0E_07: //
                    case 0x0E_09: //
                    case 0x0E_0A: //
                    case 0x0E_0B: //
                    case 0x0E_14: //

                    case 0x0F_02: // 00511e18
                    case 0x0F_03: // 00511e8c
                    case 0x0F_04: // 00511f75
                    case 0x0F_05: // 00512053
                    case 0x0F_06: // 005120e6
                    case 0x0F_07: // 00512176
                    case 0x0F_09: // 005121da
                    case 0x0F_0A: // 00512236
                    case 0x0F_0B: // 005122a4
                    case 0x0F_0C: // 0051239d
                    case 0x0F_0D: // 0051242c
                    case 0x0F_0E: // 005124f7

                    case 0x10_01: // 

                    case 0x11_01: // 0059b6b4

                    case 0x12_01: // 0052807b
                    case 0x12_02: // 005280f6
                    */
                    case 0x13_01: // 00578950 // add player to group
                        Recieve_13_01(r, clientStream);
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

                    case 0x14_01: //

                    case 0x15_01: //
                    case 0x15_02: //
                    case 0x15_03: //
                    case 0x15_04: //

                    case 0x16_01: //
                    case 0x16_02: // start tutorial?

                    case 0x17_01: // 0053a183
                    case 0x17_02: // 
                    case 0x17_03: // 
                    case 0x17_04: // 
                    case 0x17_05: // 
                    case 0x17_06: // 

                    case 0x19_01: // 
                    case 0x19_02: // 
                    case 0x19_03: // 

                    case 0x1C_01: //
                    case 0x1C_02: //
                    case 0x1C_03: //
                    case 0x1C_04: //
                    case 0x1C_0A: //

                    case 0x1F_01: // 
                    case 0x1F_02: // 
                    case 0x1F_03: // 
                    case 0x1F_04: // 
                    case 0x1F_06: // 
                    case 0x1F_07: // 
                    case 0x1F_0B: // 

                    case 0x20_03: //
                    case 0x20_04: //
                    
                    case 0x21_03: // 00538ce8
                    
                    case 0x22_03: // 
                    case 0x22_04: // 
                    case 0x22_05: // 
                    case 0x22_06: // 

                    case 0x23_01: // 005a19da
                    */

                    default:
                        Console.WriteLine("Unknown");
                        if(data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
                        break;
                }
            }

            if(account != null) {
                Console.WriteLine($"Player {account.Username} disconnected");
            }
        }

        static void Server(int port, bool lobby) {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while(true) {
                var client = server.AcceptTcpClient();

                Task.Run(() => {
                    try {
                        listenClient(client, lobby);
                    } catch(Exception e) {
                        Console.WriteLine(e);
                    }
                });
            }
        }

        static void Main(string[] args) {
            database = DataBase.Load("./db.json");
            Task.Run(() => {
                while(true) {
                    Thread.Sleep(60 * 1000); // saved once a minute
                    // not sure about thread safety eh
                    try {
                        lock(database) {
                            database.Save("./db.json");
                        }
                        Console.WriteLine("Saved Database");
                    } catch(Exception e) {
                        Console.WriteLine(e);
                    }
                }
            });

            var archive = SeanArchive.Extract("./client_table_eng.sdb");
            teleporters = T_Teleport.Load(archive.First(x => x.Name == "teleport_list.txt"));
            npcs = T_NPCName.Load(archive.First(x => x.Name == "npc_list.txt"));
            // res = T_Res.Load(archive.First(x => x.Name == "res_list.txt"));

            Server(25000, true);
        }
    }
}
