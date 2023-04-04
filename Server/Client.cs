using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using Microsoft.Extensions.Logging;
using Server.Protocols;

namespace Server;

class Client {
    public short Id { get; }
    public bool InGame { get; set; }

    public TcpClient TcpClient { get; }
    public NetworkStream Stream { get; }
    public BinaryReader Reader { get; set; }

    public string Username { get; set; }
    public ulong DiscordId { get; set; }
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

    public void Close() {
        ConnectionSource.Cancel();
    }

    public void AddExpAction(Skill skill, int level) {
        AddExp(skill, (int)(600 / Math.Pow(2, Player.Levels[(int)skill] - level + 1)));
    }

    public void AddExp(Skill skill, int gain) {
        var level = Player.Levels[(int)skill];

        if(skill != Skill.General) {
            AddExp(Skill.General, gain / 2);
        }

        var required = Program.skills[level].GetExp(skill);
        Player.Exp[(int)skill] += gain;

        if(required != 0 && Player.Exp[(int)skill] >= required) {
            Player.Levels[(int)skill]++;
            Player.Exp[(int)skill] -= required;

            if(skill == Skill.General) {
                Inventory.SendSetInventorySize(this);
            }
        }
        Protocols.Player.SendSkillChange(this, skill, true);
    }

    public ItemRef GetItem(int type, int index) {
        return GetItem((InvType)type, index);
    }
    public ItemRef GetItem(InvType type, int index) {
        return GetInv(type)[index];
    }
    public InvRef GetInv(InvType type) {
        return new InvRef(this, type);
    }

    /// <summary>Adds an item to the players inventory and sends chat notification for it</summary>
    public bool AddItem(int itemId, int count, bool notification) {
        if(itemId == 0)
            return true;
        var inv = GetInv(InvType.Player);
        return inv.AddItem(itemId, count, notification);
    }

    public bool RemoveItem(int itemId, int count) {
        if(itemId == 0)
            return true;

        // bug: removing quest requirement does not toggle dialog marker
        var inv = GetInv(InvType.Player);
        return inv.RemoveItem(itemId, count);
    }

    /// <summary>Adds an item randomly chose from a loot table to the players inventory and sends chat notification for it</summary>
    public bool AddFromLootTable(int table) {
        return GetInv(InvType.Player).AddFromLootTable(table);
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

    public void LogUnknown(int major, int minor) {
        Logger.LogWarning("[{user}] Unknown Packet {major:X2}_{minor:X2}", DiscordId, major, minor);
    }
}
