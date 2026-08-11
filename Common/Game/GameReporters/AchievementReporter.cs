using PvPOnline.Common.MainMenu.API;
using PvPOnline.Common.MainMenu.API.Achievements;
using System;
using System.Threading.Tasks;
using Terraria;

namespace PvPAdventure.Common.Game.GameReporters;

/// <summary>Posts PvPAdventure game events through PvPOnline's typed achievement API.</summary>
internal static class AchievementReporter
{
    private const string GameMode = "pvpa";

    /// <summary>
    /// Call when a single Sniper Rifle projectile confirms its second player hit.
    /// </summary>
    public static void OnSniperDoubleHit(Player shooter)
    {
        if (!PvPOnlineService.TryGetSteamId(shooter, out ulong steamId))
            return;

        _ = ReportAsync(steamId, "hit_two_one_sniper_shot");
    }

    private static async Task ReportAsync(ulong steamId, string achievementName)
    {
        try
        {
            ApiResult<ApiAchievement> result = await PvPOnlineService
                .ProgressAchievementAsync(steamId, achievementName, GameMode)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                Log.Info($"'{achievementName}' -> {steamId} progress={result.Data.Progress}/{result.Data.Target}");
            }
            else
            {
                Log.Warn($"'{achievementName}' -> {steamId} failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error reporting '{achievementName}' for {steamId}: {ex}");
        }
    }
}
