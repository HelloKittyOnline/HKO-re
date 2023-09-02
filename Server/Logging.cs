using Serilog;
using Serilog.Core;
using Serilog.Events;
using Server.Protocols;

namespace Server;

class Logging {
    private static Logger chatLogger;
    public static Logger Logger;
    public static LoggingLevelSwitch LevelSwitch;

    static Logging() {
        LevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
#if DEBUG
        LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
#endif

        Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        chatLogger = new LoggerConfiguration()
            .WriteTo.File("logs/chat.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Message:lj}{NewLine}")
            .WriteTo.Console()
            .CreateLogger();
    }

    public static void LogChat(Client client, ChatFlags flags, string message) {
        var name = flags switch {
            ChatFlags.Map => "Map",
            ChatFlags.Local => "Nrm",
            ChatFlags.Guild => "Gld",
            ChatFlags.Trade => "Trd",
            ChatFlags.Advice => "Adv",
            _ => ""
        };
        chatLogger.Information("[{type}] {mapId} {username}_{userID}: {message}", name, client.Player.CurrentMap, client.Username, client.DiscordId, message);
    }

    public static void LogChat(Client from, Client to, string message) {
        chatLogger.Information("[Prv] {username}_{user}->{otherUsername}_{other}: {message}", from.Username, from.DiscordId, to.Username, to.DiscordId, message);
    }
}