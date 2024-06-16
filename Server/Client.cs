using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using Serilog.Events;
using Server.Protocols;

namespace Server;

class Client {
    public short Id { get; }
    public bool InGame { get; set; }

    public TcpClient TcpClient { get; }
    public NetworkStream Stream { get; }

    public string Username { get; set; }
    public ulong DiscordId { get; set; }
    public PlayerData Player { get; set; }

    private CancellationTokenSource ConnectionSource;
    public CancellationToken Token => ConnectionSource.Token;
    public Task RunTask;

    private CancellationTokenSource actionToken;

    private Queue<ArraySegment<byte>> sendBuffer = new();
    private bool sending = false;

    public Client(TcpClient client) {
        Id = (short)IdManager.GetId();
        InGame = false;

        TcpClient = client;
        Stream = TcpClient.GetStream();

        ConnectionSource = new CancellationTokenSource();
        ResetTimeout();
    }

    // called from 00_63. will time out if ping does not arrive in 20 seconds
    public void ResetTimeout() {
        ConnectionSource.CancelAfter(20 * 1000);
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

    public void SetQuestFlag(int questId, byte flag) {
        lock(Player) {
            Player.QuestFlags1.TryGetValue(questId, out var val); // defaults to 0
            if((val & (1u << flag)) != 0)
                return; // skip if flag already set
            Player.QuestFlags1[questId] = val | (1u << flag);
        }
        Npc.SetQuestFlag(this, questId, flag);
    }

    public async void StartAction(Func<CancellationToken, Task> action, Action onCancel) {
        actionToken?.Cancel();

        var source = new CancellationTokenSource();
        actionToken = source;

        source.Token.Register(() => {
            try {
                onCancel();
            } catch {
                Close();
            }
            if(actionToken == source)
                actionToken = null;
        });

        try {
            await action(actionToken.Token);
        } catch(ObjectDisposedException e) when(e.ObjectName == "System.Net.Sockets.NetworkStream") {
            Logging.Logger.Information("[{username}_{userID}] Disconnected while performing action", Username, DiscordId);

            Close();
        } catch(Exception e) {
            Logging.Logger.Write(LogEventLevel.Error, e, "[{username}_{userID}] Async error", Username, DiscordId);
            Close();
        }

        // action completed
        // if the token is our own delete it
        if(actionToken == source)
            actionToken = null;
    }

    public void CancelAction() {
        actionToken?.Cancel();
    }

    public void UpdateStats() {
        if(Player != null) {
            Player.UpdateStats();
            Protocols.Player.SendPlayerHpSta(this);
        }
    }

    public void Send(ArraySegment<byte> data) {
        lock(sendBuffer) {
            sendBuffer.Enqueue(data);
            if(sending)
                return;

            sending = true;
            Task.Run(SendTask);
        }
    }

    private async Task SendTask() {
        while(true) {
            ArraySegment<byte> buffer;
            lock(sendBuffer) {
                if(sendBuffer.Count == 0) {
                    sending = false;
                    return;
                }

                buffer = sendBuffer.Dequeue();
            }

            try {
                await Stream.WriteAsync(buffer);
            } catch {
                Close();
            }
        }
    }

}
