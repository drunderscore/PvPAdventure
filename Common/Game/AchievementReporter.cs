using PvPAdventure.Common.Statistics;
using PvPHub.Common.MainMenu.API;
using PvPHub.Common.MainMenu.API.Achievements;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game;

/// <summary>
/// Bridges PvPAdventure game events to PvPHub's achievement progress API.
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
        if (!ModLoader.TryGetMod("PvPHub", out _))
            return;

        if (!TryGetSteamId(player, out ulong steamId))
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
        if (!ModLoader.TryGetMod("PvPHub", out _))
            return;

        Team winningTeam = FindWinningTeam(pointsManager);
        if (winningTeam == Team.None)
        {
            Log.Warn("No winning team found at match end — win achievements skipped.");
            return;
        }

        foreach (Player player in Main.ActivePlayers)
        {
            if ((Team)player.team != winningTeam)
                continue;

            if (!TryGetSteamId(player, out ulong steamId))
                continue;

            // Report a single win against every cumulative win counter.
            // Backend increments each until its respective target is reached.
            _ = ReportAsync(steamId, "win_1");
            _ = ReportAsync(steamId, "win_5");
            _ = ReportAsync(steamId, "win_25");
            _ = ReportAsync(steamId, "win_50");
        }
    }

    /// <summary>
    /// Call when a single Sniper Rifle projectile confirms its second player hit.
    /// See <see cref="SniperMultiHitTracker"/> for the projectile-side hook.
    /// </summary>
    public static void OnSniperDoubleHit(Player shooter)
    {
        if (!ModLoader.TryGetMod("PvPHub", out _))
            return;

        if (!TryGetSteamId(shooter, out ulong steamId))
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

    // Mirrors OfficialMatchReporter.TryGetPlayerSteamId
    private static bool TryGetSteamId(Player player, out ulong steamId)
    {
        ulong? id = player.GetModPlayer<PvPHub.Common.Authentication.AuthenticatedPlayer>().SteamId;

        if (id.HasValue && id.Value != 0 && id.Value <= (ulong)long.MaxValue)
        {
            steamId = id.Value;
            return true;
        }

        steamId = 0;
        return false;
    }

    private static Team FindWinningTeam(PointsManager pointsManager)
    {
        Team winner = Team.None;
        int topPoints = 0;

        foreach ((Team team, int points) in pointsManager.Points)
        {
            if (team != Team.None && points > topPoints)
            {
                topPoints = points;
                winner = team;
            }
        }

        return winner;
    }
}

