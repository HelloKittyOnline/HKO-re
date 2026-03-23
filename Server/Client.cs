using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Extractor;
using Serilog.Events;
using Server.Protocols;

namespace Server;

class Client {
    public readonly short Id;
    public bool InGame { get; set; }

    public readonly TcpClient TcpClient;

    public string Username { get; set; }
    public ulong DiscordId { get; set; }
    public PlayerData Player { get; set; }

    private readonly CancellationTokenSource ConnectionSource;
    public CancellationToken Token => ConnectionSource.Token;
    public Task RunTask;

    private CancellationTokenSource actionToken;

    private readonly SemaphoreSlim sem = new(0);
    private readonly Queue<ArraySegment<byte>> sendBuffer = new();

    public readonly Lock Lock = new();

    public Client(TcpClient client) {
        Id = (short)IdManager.GetId();
        InGame = false;

        TcpClient = client;

        ConnectionSource = new CancellationTokenSource();
        ResetTimeout();
        Task.Run(SendTask, Token);
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
        Debug.Assert(count != 0);
        var inv = GetInv(InvType.Player);
        return inv.AddItem(itemId, count, notification);
    }

    /// <summary>Adds an item to the players inventory and sends chat notification for it</summary>
    public bool AddItem(InventoryItem item, bool notification) {
        Debug.Assert(item.Id != 0 && item.Count != 0);
        if(item.Id == 0)
            return true;
        return GetInv(InvType.Player).AddItem(item, notification);
    }

    public bool RemoveItem(int itemId, int count) {
        return GetInv(InvType.Player).RemoveItem(itemId, count);
    }

    /// <summary>Adds an item randomly chose from a loot table to the players inventory and sends chat notification for it</summary>
    public bool AddFromLootTable(int table) {
        return GetInv(InvType.Player).AddFromLootTable(table);
    }

    public void SetQuestFlag(int questId, byte flag) {
        lock(Lock) {
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
                Logging.Logger.Error("[{username}_{userID}] onCancel failed", Username, DiscordId);
                Close();
            }
            if(actionToken == source)
                actionToken = null;
        });

        try {
            await action(source.Token);
        } catch(Exception e) {
            if(source.Token.IsCancellationRequested) {
                Debug.Assert(e is OperationCanceledException);
            } else {
                Logging.Logger.Write(LogEventLevel.Error, e, "[{username}_{userID}] Async error", Username, DiscordId);
                Close();
            }
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
        Player.UpdateStats();
        Protocols.Player.SendPlayerHpSta(this);
    }
    public void UpdateEquip() {
        Protocols.Player.SendPlayerAtt(Player.Map.Players, this);
        UpdateStats();
    }

    public void Send(ArraySegment<byte> data) {
        lock(sendBuffer) {
            sendBuffer.Enqueue(data);
        }
        sem.Release();
    }

    private async Task SendTask() {
        try {
            while(true) {
                await sem.WaitAsync(Token);

                ArraySegment<byte> buffer;
                lock(sendBuffer) {
                    Debug.Assert(sendBuffer.Count > 0, "sendBuffer.Count has to be > 0 before semaphore fires");
                    buffer = sendBuffer.Dequeue();
                }

                await TcpClient.Client.SendAsync(buffer, Token);
            }
        } catch {
            // IOException, ObjectDisposedException or OperationCanceledException
            Close();
        }
    }
}
