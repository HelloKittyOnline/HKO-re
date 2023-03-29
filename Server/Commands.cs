using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Server.Protocols;

namespace Server;

[AttributeUsage(AttributeTargets.Method)]
class Command : Attribute {
    public readonly string Usage;
    public readonly string Description;

    public Command(string usage, string description) {
        Usage = usage;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Method)]
class ChatCommand : Attribute {
    public readonly string Usage;
    public readonly string Description;
    public readonly bool AdminOnly;

    public ChatCommand(string usage, string description, bool adminOnly) {
        Usage = usage;
        Description = description;
        AdminOnly = adminOnly;
    }
}

static class Commands {
    private delegate void CommandFunc(string[] args);
    private delegate void ChatCommandFunc(Client client, string[] args);

    private static Dictionary<string, CommandFunc> commands = new();
    private static Dictionary<string, (bool admin, ChatCommandFunc func)> chatCommands = new();

    private static string helpString;

    static Commands() {
        int usageLength = 0;

        foreach(var method in typeof(Commands).GetMethods()) {
            var cmd = method.GetCustomAttribute<ChatCommand>();
            if(cmd == null)
                continue;

            chatCommands[method.Name.ToLower()] = (cmd.AdminOnly, method.CreateDelegate<ChatCommandFunc>());
        }

        foreach(var method in typeof(Commands).GetMethods()) {
            var cmd = method.GetCustomAttribute<Command>();
            if(cmd == null)
                continue;

            commands[method.Name.ToLower()] = method.CreateDelegate<CommandFunc>();
            usageLength = Math.Max(usageLength, cmd.Usage.Length);
        }

        helpString = "";
        foreach(var method in typeof(Commands).GetMethods()) {
            var cmd = method.GetCustomAttribute<Command>();
            if(cmd == null)
                continue;

            helpString += $"  {cmd.Usage.PadRight(usageLength)} - {cmd.Description}\n";
        }
    }

    public static void RunConsole() {
        Task.Run(() => {
            while(true) {
                var command = Console.ReadLine();
                if(command == null)
                    break;

                var elements = SplitCommand(command);
                if(elements.Length == 0)
                    return;

                Handle(elements);
            }
        });
    }

    private static string[] SplitCommand(string message) {
        // TODO: make better
        return message.Split(" ");
    }

    public static void Handle(string[] args) {
        try {
            var name = args[0];

            if(commands.TryGetValue(name.ToLower(), out var cmd)) {
                cmd(args);
            } else {
                Console.WriteLine($"Unknown command: \"{name}\"");
            }
        } catch(Exception e) {
            Console.WriteLine(e);
        }
    }

    public static bool HandleChat(Client client, string message) {
        if(!message.StartsWith('\\'))
            return false;

        try {
            var args = SplitCommand(message);

            // todo: if admin check through server commands
            if(chatCommands.TryGetValue(args[0][1..], out var cmd)) {
                if(!cmd.admin) { // TODO: check player admin
                    cmd.func(client, args);
                }
            } else {
                // TODO: send error chat message to client
            }
        } catch(Exception e) {
            client.Logger.LogError(e, "Error handling chat command {message}", message);
        }

        return true;
    }

    private static Client FindPlayer(string name) {
        return Program.clients.FirstOrDefault(x => x.Username == name);
    }

    [Command("help", "display this info")]
    public static void Help(string[] args) {
        Console.Write(helpString);
    }

    [Command("online", "Displays the count and names of online users")]
    public static void Online(string[] args) {
        Console.WriteLine($"There are currently {Program.clients.Count} players online");

        foreach(var client in Program.clients) {
            var ip = client.TcpClient.Client.RemoteEndPoint;

            if(client.Username == null) {
                Console.WriteLine(ip);
            } else if(client.Player == null || !client.InGame) {
                Console.WriteLine($"{ip} - {client.DiscordId} - {client.Username}");
            } else {
                Console.WriteLine($"{ip} - {client.DiscordId} - {client.Username} - {client.Player.Name}");
            }
        }
    }

    [Command("totalUsers", "Displays the count of registered users")]
    public static void TotalUsers(string[] args) {
        Console.WriteLine($"There are {Database.GetRegisteredUserCount()} registered users");
    }

    [Command("giveItem [player] [itemId] [count]", "give player item")]
    public static void GiveItem(string[] args) {
        if(args.Length != 4) {
            Console.WriteLine("Missing parameter");
            return;
        }

        var client = FindPlayer(args[1]);
        if(client == null) {
            Console.WriteLine($"Unknown player {args[1]}");
            return;
        }

        client.AddItem(int.Parse(args[2]), int.Parse(args[3]), true);
    }

    [Command("clear", "Clear the console window")]
    public static void Clear(string[] args) {
        Console.Clear();
    }

    [Command("kick [ip|username]", "kick player")]
    public static void Kick(string[] args) {
        if(args.Length <= 1) {
            Console.WriteLine("Please set a valid player");
            return;     
        }
        Client client = null;
        if(IPEndPoint.TryParse(args[1], out var ip)) { 
            client = Program.clients.FirstOrDefault(x => x.TcpClient.Client.RemoteEndPoint.Equals(ip));
        } else {
            client = FindPlayer(args[1]);
        }
        if(client == null) {
            Console.WriteLine("Could not find player");
            return;
        }
        client.Close();         
    }

    [ChatCommand("stuck", "Teleports you back to sanrio harbour", false)]
    public static void Stuck(Client client, string[] args) {
        var player = client.Player;

        // delete players from old map
        Player.SendDeletePlayer(player.Map.Players, client);

        player.PositionX = 7705;
        player.PositionY = 6007;
        player.CurrentMap = 8;

        Player.ChangeMap(client);
    }
}
