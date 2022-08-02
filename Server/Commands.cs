using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

static class Commands {
    private delegate void CommandFunc(string[] args);
    private static Dictionary<string, CommandFunc> commands = new();
    private static string helpString;

    static Commands() {
        int usageLength = 0;

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

                var elements = command.Split(" ");
                if(elements.Length == 0)
                    return;

                Commands.Handle(elements);
            }
        });
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

    [Command("help", "display this info")]
    public static void Help(string[] args) {
        Console.Write(helpString);
    }

    [Command("online", "Displays the count and names of online users")]
    public static void Online(string[] args) {
        Console.WriteLine($"There are currently {Program.clients.Count} players online");
        if(Program.clients.Count != 0)
            Console.WriteLine(string.Join(" ", Program.clients.Where(x => x.InGame).Select(x => x.Player.Name)));
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

        var player = Program.clients.FirstOrDefault(x => x.Player?.Name == args[1]);
        if(player == null) {
            Console.WriteLine($"Unknown player {args[1]}");
            return;
        }

        player.AddItem(int.Parse(args[2]), int.Parse(args[3]));
    }

    [Command("clear", "Clear the console window")]
    public static void Clear(string[] args) {
        Console.Clear();
    }
}
