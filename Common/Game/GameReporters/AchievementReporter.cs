using PvPAdventure.Common.Statistics;
using PvPHub.Common.MainMenu.API;
using PvPHub.Common.MainMenu.API.Achievements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
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

    private static readonly (string AchievementName, int RequiredMatchKills)[] MatchKillAchievements =
    [
        ("kill_1", 1),
        ("kill_one_match_50", 50),
        ("kill_one_match_100", 100)
    ];

    private static readonly string[] WinAchievements =
    [
        "win_1",
        "win_5",
        "win_25",
        "win_50"
    ];

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

        foreach ((string achievementName, int requiredMatchKills) in MatchKillAchievements)
        {
            if (matchKills == requiredMatchKills)
                _ = ReportAsync(steamId, achievementName);
        }
    }

    /// <summary>
    /// Call once when the match ends. Posts one win tick to every cumulative win achievement
    /// for each player on the single winning team.
    /// </summary>
    public static void OnMatchEnded(PointsManager pointsManager)
    {
        if (!PvPHubCompat.IsPvPHubLoaded || Main.netMode != NetmodeID.Server)
            return;

        if (!TryGetSingleWinningTeam(pointsManager, out Team winningTeam))
            return;

        foreach (Player player in Main.ActivePlayers)
        {
            if (player == null || !player.active || (Team)player.team != winningTeam)
                continue;

            if (!PvPHubCompat.TryGetSteamId(player, out ulong steamId))
                continue;

            ReportAll(steamId, WinAchievements);
        }
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

    private static bool TryGetSingleWinningTeam(PointsManager pointsManager, out Team winningTeam)
    {
        winningTeam = Team.None;

        if (pointsManager == null)
            return false;

        var leadingTeams = pointsManager.Points
            .Where(entry => entry.Key != Team.None)
            .Where(entry => entry.Value > 0)
            .GroupBy(entry => entry.Value)
            .OrderByDescending(group => group.Key)
            .FirstOrDefault()
            ?.Select(entry => entry.Key)
            .ToArray();

        if (leadingTeams == null || leadingTeams.Length != 1)
            return false;

        winningTeam = leadingTeams[0];
        return true;
    }

    private static void ReportAll(ulong steamId, IEnumerable<string> achievementNames)
    {
        foreach (string achievementName in achievementNames)
            _ = ReportAsync(steamId, achievementName);
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

