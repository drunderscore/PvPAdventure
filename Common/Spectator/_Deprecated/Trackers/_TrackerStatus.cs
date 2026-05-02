using PvPAdventure.Core.Config;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator._Deprecated.Trackers;

/// <summary>
/// Whether we track stats for spectating.
/// </summary>
public static class _TrackerStatus
{
    public static bool IsEnabled
    {
        get
        {
            var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
            if (spectatorConfig == null)
            {
                DebugLog.Warn("Giga error, spectatorConfig is null when checking if spectator stats are enabled. Returning false.");
                return false;
            }

            return spectatorConfig.AllowSpectating;
        }
    }
}
