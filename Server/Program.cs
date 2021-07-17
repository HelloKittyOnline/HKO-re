using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    class Program {
        #region Responses
        // (0,1)
        static void SendLobby(Stream clientStream, bool lobby) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer");

            // idk
            b.Add((short)0);

            // idk
            b.Add((short)1);
            // b.Add((byte)0);

            b.Send(clientStream);
        }

        // (0,2,1)
        static void SendAcceptClient(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x2); // second switch
            b.Add((byte)0x1); // third switch

            b.AddString("");
            b.AddString(""); // appended to username??
            b.AddString(""); // blowfish encrypted stuff???

            b.Send(clientStream);
        }

        // (0,4)
        static void SendServerList(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x4); // second switch

            // some condition?
            b.Add((short)0);

            // server count
            b.Add(1);
            {
                b.Add(1); // server number
                b.AddWstring("Test Sevrer");

                // world count
                b.Add(1);
                {
                    b.Add(1); // wolrd number
                    b.AddWstring("Test World");
                    b.Add(0); // world status
                }
            }


            b.Send(clientStream);
        }

        // (0,5)
        static void Idk05(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0x5); // second switch

            b.Add(1); // count

            b.Add(1); // id??
            b.AddString("Test server");

            b.Send(clientStream);
        }

        // (0, 11)
        static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xB); // second switch

            b.Add(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1"); // address
            b.Add((short)12345); // port

            b.Send(clientStream);
        }

        // (0, 12)
        static void Idk0C(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x0); // first switch
            b.Add((byte)0xB); // second switch

            b.Add((byte)0); // 0-7 switch

            b.Send(clientStream);
        }

        // (2, 9)
        static void Idk29(Stream clientStream) {
            var b = new PacketBuilder();

            b.Add((byte)0x2); // first switch
            b.Add((byte)0x9); // second switch

            b.Add((int)0);
            b.Add((short)0);
            b.Add((short)0);
            b.Add((byte)0);

            for (int i = 0; i < 64; i++) {
                b.Add((byte)0);
            }

            b.Send(clientStream);
        }
        #endregion

        #region Packet handlers
        static void AcceptClient(BinaryReader req, Stream res) {
            // idk - skip 5 bytes
            var val = req.ReadBytes(5);

            bool nonsense = val[0] != 0x11;
            if (nonsense) {
                req.ReadBytes(2);
            }

            // somehow the offsets change randomly?
            int nameLength = req.ReadByte();
            var userName = Encoding.ASCII.GetString(req.ReadBytes(nameLength));

            // empty space
            req.ReadBytes(64 - nameLength - (nonsense ? 3 : 0));

            int pwLength = req.ReadByte();
            var password = Encoding.ASCII.GetString(req.ReadBytes(pwLength)); // compressed through some weird method

            Console.WriteLine($"{userName}, {password}");

            SendAcceptClient(res);
        }

        static void ServerList(BinaryReader req, Stream res) {
            var count = req.ReadInt32();

            for (int i = 0; i < count; i++) {
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

        static void Idk(BinaryReader req, Stream res) {
            var idk1 = Encoding.ASCII.GetString(req.ReadBytes(req.ReadByte()));
            var idk2 = req.ReadInt32();

            Debugger.Break();
        }
        #endregion

        static void listenClient(TcpClient socket, bool lobby) {
            Console.WriteLine($"Client {socket.Client.RemoteEndPoint} {socket.Client.LocalEndPoint}");

            var clientStream = socket.GetStream();
            var reader = new BinaryReader(clientStream);

            SendLobby(clientStream, lobby);

            while (true) {
                var head = reader.ReadBytes(3);
                if (head.Length == 0) {
                    break;
                }

                var length = reader.ReadUInt16();
                var data = reader.ReadBytes(length); // only done to log packets

                var ms = new MemoryStream(data);
                var r = new BinaryReader(ms);

                var id = (r.ReadByte() << 8) | r.ReadByte();

                // S -> C:   01: SendLobby         // lobby server
                // S <- C: 0001: AcceptClient      // send login details
                // S -> C:  021: SendAcceptClient  // check login details
                // S <- C: 0004: ServerList        // request server list (after License accept)
                // S -> C:   04: SendServerList    // send server list
                // S <- C: 0003: SelectServer      
                // (optional) S -> C:   0B: SendChangeServer
                // S -> C:   01: SendLobby         // realm server?
                // S <- C: 000B: Idk
                // S -> C: ????

                switch (id) {
                    case 0x0001: // Auth
                        Console.WriteLine(BitConverter.ToString(data, 2));
                        AcceptClient(r, clientStream); break;
                    case 0x0003: // after user selected world
                        SelectServer(r, clientStream); break;
                    case 0x0004: // list of languages? sent after lobbyServer
                        ServerList(r, clientStream); break;
                    case 0x000B: // source location 0059b14a // sent after realmServer
                        Idk(r, clientStream); break;
                    case 0x0063: // source location 0059b253
                        Ping(r, clientStream); break;
                    case 0x0010: // source location 0059b1ae // has something to do with T_LOADScreen

                    case 0x0202: // source location 005df036 // sent after (2,9)
                    case 0x021A: // source location 005df655 // sent after 0x202

                    case 0x0401:
                    case 0x0402:
                    case 0x0403:
                    case 0x0404:
                    case 0x0405:
                    case 0x0407:

                    case 0x0701:
                    case 0x0704:

                    case 0x0C03:
                    case 0x0C07:
                    case 0x0C08:
                    case 0x0C09:

                    case 0x0D02:
                    case 0x0D03:
                    case 0x0D05:
                    case 0x0D06:
                    case 0x0D07:
                    case 0x0D09:
                    case 0x0D0A:
                    case 0x0D0B:
                    case 0x0D0C:
                    case 0x0D0D:
                    case 0x0D0E:
                    case 0x0D0F:
                    case 0x0D10:
                    case 0x0D11:
                    case 0x0D12:
                    case 0x0D13:

                    case 0x0F02: // source location 00511e18
                    case 0x0F03: // source location 00511e8c
                    case 0x0F04: // source location 00511f75
                    case 0x0F05: // source location 00512053
                    case 0x0F06: // source location 005120e6
                    case 0x0F07: // source location 00512176
                    case 0x0F09: // source location 005121da
                    case 0x0F0A: // source location 00512236
                    case 0x0F0B:
                    case 0x0F0C:
                    case 0x0F0D:
                    case 0x0F0E:

                    case 0x1101: // source location 0059b6b4

                    case 0x1201:
                    case 0x1202:

                    default:
                        Console.WriteLine($"Unknown: {id}");
                        if (data.Length > 2) Console.WriteLine(BitConverter.ToString(data, 2));
                        Debugger.Break();
                        break;
                }
            }
        }

        static void Server(int port, bool lobby) {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while (true) {
                var client = server.AcceptTcpClient();

                Task.Run(() => {
                    try {
                        listenClient(client, lobby);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine("Thread 1 done");
                });
            }
        }

        static void Main(string[] args) {
            Task.WaitAll(
                Task.Run(() => Server(25000, true))
            //, Task.Run(() => Server(12345, false))
            );
        }
    }
}
