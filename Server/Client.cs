using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Server.Protocols;

namespace Server {
    class Client {
        public short Id { get; }
        public bool InGame { get; set; }

        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public BinaryReader Reader { get; set; }

        public string Username { get; set; }
        public PlayerData Player { get; set; }

        private CancellationTokenSource ConnectionSource;
        public CancellationToken Token => ConnectionSource.Token;
        public Task RunTask;

        public ILogger Logger { get; set; }

        private CancellationTokenSource actionToken;

        public Client(TcpClient client) {
            Id = (short)IdManager.GetId();
            InGame = false;

            TcpClient = client;
            Stream = TcpClient.GetStream();

            ConnectionSource = new CancellationTokenSource();
            Logger = Program.loggerFactory.CreateLogger("Client");
        }

        ~Client() {
            IdManager.FreeId(Id);
        }

        public void Close() {
            ConnectionSource.Cancel();
        }

        public bool AddItem(int item, int count) {
            var pos = Player.AddItem(item, count);
            if(pos == -1) {
                // inventory full
                return false;
            } else {
                Inventory.SendGetItem(this, Player.Inventory[pos], (byte)(pos + 1), true);
                return true;
            }
        }

        public bool RemoveItem(int itemId, int count) {
            Debug.Assert(count < 256);

            for(int i = 0; i < Player.InventorySize; i++) {
                if(Player.Inventory[i].Id == itemId) {
                    var _count = Player.Inventory[i].Count;

                    if(_count > count) {
                        Player.Inventory[i].Count -= (byte)count;
                        Inventory.SendSetItem(this, Player.Inventory[i], i + 1);
                        return true;
                    }
                    if(_count == count) {
                        Player.Inventory[i] = InventoryItem.Empty;
                        Inventory.SendSetItem(this, InventoryItem.Empty, i + 1);
                        return true;
                    }

                    // remove partial item and keep going
                    Player.Inventory[i] = InventoryItem.Empty;
                    count -= _count;
                    Inventory.SendSetItem(this, InventoryItem.Empty, i + 1);
                }
            }

            // should never happen
            // could not remove all items
            Debug.Assert(false);
            return false;
        }


        public void StartAction(Action<CancellationToken> action, Action onCancel) {
            actionToken?.Cancel();

            var source = new CancellationTokenSource();
            actionToken = source;

            source.Token.Register(() => {
                onCancel();
                if(actionToken == source)
                    actionToken = null;
            });

            Task.Run(() => {
                action(actionToken.Token);

                // action completed
                // if the token is our own delete it
                if(actionToken == source)
                    actionToken = null;
            });
        }

        public void CancelAction() {
            actionToken?.Cancel();
        }

        public byte ReadByte() { return Reader.ReadByte(); }
        public short ReadInt16() { return Reader.ReadInt16(); }
        public ushort ReadUInt16() { return Reader.ReadUInt16(); }
        public int ReadInt32() { return Reader.ReadInt32(); }
        public byte[] ReadBytes(int count) { return Reader.ReadBytes(count); }

        public string ReadWString() {
            return Encoding.Unicode.GetString(ReadBytes(ReadUInt16()));
        }
        public string ReadString() {
            return PacketBuilder.Window1252.GetString(ReadBytes(ReadByte()));
        }

        public void UpdateStats() {
            if(Player != null) {
                Player.UpdateStats();
                Protocols.Player.SendPlayerHpSta(this);
            }
        }
    }
}