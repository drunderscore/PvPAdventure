using log4net;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

/// <summary>
/// A static logging helper for PvPAdventure.
/// Used to minimize boilerplate 
/// and improve debugging by providing class names to log messages.
/// Example usage: Log.Debug("Your debug message.");
/// Output: [YourCallerFile] Your debug message.
/// </summary>
public static class Log
{
    public static ILog Base
    {
        get
        {
            var m = ModContent.GetInstance<PvPAdventure>();
            return (m != null && m.Logger != null) ? m.Logger : LogManager.GetLogger("PvPAdventure");
        }
    }

    /// <summary>
    /// Sends a debug Terraria chat message, e.g [CLIENT/FileName: Your Message]
    /// </summary>
    public static void Chat(object message, [CallerFilePath] string file = "")
    {
        // Always send the message to the log (client.log/server.log)
        Debug(message);

        // Check if debug messages are enabled in config
        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.ShowDebugMessages)
            return;

        // Sanitize file name
        string fileName = Path.GetFileNameWithoutExtension(file);

        // Truncate
        if (fileName.Length > 17)
            fileName = fileName[..17] + "..";

        string text = "";

        if (Main.netMode == NetmodeID.Server)
        {
            text += $"[SERVER/{fileName}]: {message}";
            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(text), color: Color.White, playerId: Main.LocalPlayer.whoAmI);
        }
        else
        {
            text += $"[CLIENT/{fileName}]: {message}";
            Main.NewText(text, Color.White);
        }
    }

    public static void Info(object message, [CallerFilePath] string file = "")
        => Base.Info($"[{Class(file)}] {message}");

    public static void Debug(object message, [CallerFilePath] string file = "")
        => Base.Debug($"[{Class(file)}] {message}");

    public static void Warn(object message, [CallerFilePath] string file = "")
        => Base.Warn($"[{Class(file)}] {message}");

    public static void Error(object message, [CallerFilePath] string file = "")
        => Base.Error($"[{Class(file)}] {message}");

    /// Returns the file name without its extension from the specified file path.
    private static string Class(string file) => Path.GetFileNameWithoutExtension(file);

    /// Provides a logging wrapper that prefixes all log messages with a specified string.
    public readonly struct Prefixed
    {
        private readonly ILog _log;
        private readonly string _p;
        public Prefixed(ILog log, string prefix) { _log = log; _p = prefix; }
        public void Info(object m) => _log.Info($"[{_p}] {m}");
        public void Debug(object m) => _log.Debug($"[{_p}] {m}");
        public void Warn(object m) => _log.Warn($"[{_p}] {m}");
        public void Error(object m) => _log.Error($"[{_p}] {m}");
    }
}