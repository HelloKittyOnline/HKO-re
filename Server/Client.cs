using System.IO;
using System.Net.Sockets;
using System.Text;
using Server.Protocols;

namespace Server {
    class Client {
        public short Id { get; }
        public bool InGame { get; set; }

        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public BinaryReader Reader { get; set; }

        public Account Account { get; set; }
        public PlayerData Player => Account.PlayerData;

        public Client(TcpClient client) {
            Id = (short)IdManager.GetId();
            InGame = false;
            
            TcpClient = client;
            Stream = TcpClient.GetStream();
        }

        ~Client() {
            IdManager.FreeId(Id);
        }

        public void AddItem(int item, int count) {
            var pos = Player.AddItem(item, count);
            if(pos == -1) {
                // inventory full
            } else {
                Inventory.SendGetItem(Stream, Player.Inventory[pos], (byte)(pos + 1), true);
            }
        }

        public byte ReadByte() { return Reader.ReadByte(); }
        public short ReadInt16() { return Reader.ReadInt16(); }
        public ushort ReadUInt16() { return Reader.ReadUInt16(); }
        public int ReadInt32() { return Reader.ReadInt32(); }
        public byte[] ReadBytes(int count) { return Reader.ReadBytes(count); }

        public string ReadWString() {
            return Encoding.Unicode.GetString(ReadBytes(ReadUInt16()));
        }
    }
}