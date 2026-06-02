using PvPAdventure.Common.Statistics;
using PvPHub.Common.MainMenu.API;
using PvPHub.Common.MainMenu.API.Achievements;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.GameReporters;

/// <summary>
/// Posts PvPAdventure game events to PvPHub's achievement progress API.
/// </summary>
[JITWhenModsEnabled("PvPHub")]
[ExtendsFromMod("PvPHub")]
internal static class AchievementReporter
{
    private const string GameMode = "pvpa";

    /// <summary>
    /// Call every time a player's per-match kill count increments.
    /// <paramref name="matchKills"/> must already reflect the new value.
    /// </summary>
    public static void OnKillRecorded(Player player, int matchKills)
    {
        if (!PvPHubCompat.IsPvPHubLoaded)
            return;

        if (!PvPHubCompat.TryGetSteamId(player, out ulong steamId))
            return;

        if (matchKills == 1)
            _ = ReportAsync(steamId, "kill_1");

        if (matchKills == 50)
            _ = ReportAsync(steamId, "kill_one_match_50");

        if (matchKills == 100)
            _ = ReportAsync(steamId, "kill_one_match_100");
    }

    public static void OnMatchEnded(PointsManager pointsManager)
    {
        // Win achievements are protected by PvPHub and must be derived from official match records.
    }

    /// <summary>
    /// Call when a single Sniper Rifle projectile confirms its second player hit.
    /// See <see cref="SniperMultiHitTracker"/> for the projectile-side hook.
    /// </summary>
    public static void OnSniperDoubleHit(Player shooter)
    {
        if (!PvPHubCompat.IsPvPHubLoaded)
            return;

        if (!PvPHubCompat.TryGetSteamId(shooter, out ulong steamId))
            return;

        _ = ReportAsync(steamId, "hit_two_one_sniper_shot");
    }

    private static async Task ReportAsync(ulong steamId, string achievementName)
    {
        try
        {
            AchievementRef achievement = new(achievementName, GameMode);
            ApiResult<ApiAchievement> result =
                await AchievementsApi.ProgressAchievementAsync(steamId, achievement).ConfigureAwait(false);

            if (result.IsSuccess)
                Log.Info($"'{achievementName}' → {steamId}  progress={result.Data.Progress}/{result.Data.Target}");
            else
                Log.Warn($"'{achievementName}' → {steamId}  failed: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error reporting '{achievementName}' for {steamId}: {ex}");
        }
    }

}

