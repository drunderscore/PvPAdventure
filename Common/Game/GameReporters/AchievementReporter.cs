using System;
using System.Threading.Tasks;
using Terraria;

namespace PvPAdventure.Common.Game.GameReporters;

/// <summary>Posts PvPAdventure game events through PvPHub's Mod.Call API.</summary>
internal static class AchievementReporter
{
    private const string GameMode = "pvpa";

    /// <summary>
    /// Call when a single Sniper Rifle projectile confirms its second player hit.
    /// </summary>
    public static void OnSniperDoubleHit(Player shooter)
    {
        if (!PvPHubCompat.TryGetSteamId(shooter, out ulong steamId))
            return;

        _ = ReportAsync(steamId, "hit_two_one_sniper_shot");
    }

    private static async Task ReportAsync(ulong steamId, string achievementName)
    {
        try
        {
            PvPHubCallResult result = await PvPHubCompat
                .ProgressAchievementAsync(steamId, achievementName, GameMode)
                .ConfigureAwait(false);

            if (result.Success)
            {
                result.TryGetDataUInt32("progress", out uint progress);
                result.TryGetDataUInt32("target", out uint target);
                Log.Info($"'{achievementName}' -> {steamId} progress={progress}/{target}");
            }
            else
            {
                Log.Warn($"'{achievementName}' -> {steamId} failed: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error reporting '{achievementName}' for {steamId}: {ex}");
        }
    }
}
