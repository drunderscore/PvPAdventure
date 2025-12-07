using System.IO;
using System.Runtime.CompilerServices;
using log4net;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Helpers;

/// <summary>
/// A static logging helper for PvPAdventure.
/// Example usage: Log.Info("This is an info message.");
/// Example usage: Log.Warn("This is a warning message.");
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

    public static void Info(object message, [CallerFilePath] string file = "")
        => Base.Info($"[{Class(file)}] {message}");

    public static void Debug(object message, [CallerFilePath] string file = "")
        => Base.Debug($"[{Class(file)}] {message}");

    public static void Warn(object message, [CallerFilePath] string file = "")
        => Base.Warn($"[{Class(file)}] {message}");

    public static void Error(object message, [CallerFilePath] string file = "")
        => Base.Error($"[{Class(file)}] {message}");

    public static Prefixed For<T>() => new(Base, typeof(T).Name);
    public static Prefixed ForCaller([CallerFilePath] string file = "") => new(Base, Class(file));

    private static string Class(string file) => Path.GetFileNameWithoutExtension(file);

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
