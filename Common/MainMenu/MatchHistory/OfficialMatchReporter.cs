using PvPAdventure.Common.Statistics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PvPAdventure.Common.Authentication;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using PvPAdventure.Common.MainMenu.API;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

internal static class OfficialMatchReporter
{
    public static void PostCompletedMatch(DateTime startUtc, DateTime endUtc)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        MatchResult match = BuildMatchResult(startUtc, endUtc);
        LogMatchResult(match);
        _ = PostMatchSafeAsync(match);
    }

    private static async Task PostMatchSafeAsync(MatchResult match)
    {
        try
        {
            ApiResult<string> result = await MatchApi.PostOfficialMatchAsync(match).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                Log.Error($"[OfficialMatchReporter] Failed to post match. Status={(int)result.Status}, Error={result.ErrorMessage}");
                return;
            }

            if (string.IsNullOrWhiteSpace(result.Data))
            {
                Log.Info("[OfficialMatchReporter] Posted match successfully.");
                return;
            }

            Log.Info($"[OfficialMatchReporter] Posted match successfully. MatchId={result.Data}");
        }
        catch (Exception ex)
        {
            Log.Error($"[OfficialMatchReporter] Unexpected error while posting match: {ex}");
        }
    }

    private static MatchResult BuildMatchResult(DateTime startUtc, DateTime endUtc)
    {
        PointsManager pointsManager = ModContent.GetInstance<PointsManager>();

        TeamPoints[] teamPoints = BuildTeamPointsArray(pointsManager);
        PlayerKD[] players = BuildPlayerKDArray();
        TeamBossCompletion[] bosses = BuildBossCompletionArray(pointsManager);
        ulong localSteamId = SteamAuthentication.ClientSteamId.m_SteamID;
        Log.Debug("Building match with steamID: " + localSteamId.ToString());

        return new MatchResult(
            start: DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            end: DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
            win: false,
            localSteamId: 0,
            teamPoints: teamPoints,
            players: players,
            bossScoreboard: bosses);
    }

    private static TeamPoints[] BuildTeamPointsArray(PointsManager pointsManager)
    {
        List<TeamPoints> result = [];

        foreach ((Team team, int points) in pointsManager.Points)
        {
            if (team == Team.None)
                continue;

            result.Add(new TeamPoints(team, points));
        }

        return [.. result];
    }

    private static PlayerKD[] BuildPlayerKDArray()
    {
        List<PlayerKD> result = [];

        foreach (Player player in Main.ActivePlayers)
        {
            StatisticsPlayer statsPlayer = player.GetModPlayer<StatisticsPlayer>();
            Team team = (Team)player.team;

            ulong steamId = 0;
            if (!TryGetPlayerSteamId(player, out steamId))
                steamId = 0;

            result.Add(new PlayerKD(team, steamId, player.name, statsPlayer.Kills, statsPlayer.Deaths));
        }

        return [.. result];
    }

    private static TeamBossCompletion[] BuildBossCompletionArray(PointsManager pointsManager)
    {
        List<TeamBossCompletion> result = [];

        foreach ((Team team, ISet<short> downedNpcs) in pointsManager.DownedNpcs)
        {
            if (team == Team.None)
                continue;

            foreach (short bossId in downedNpcs)
                result.Add(new TeamBossCompletion(bossId, team));
        }

        return [.. result];
    }

    private static void LogMatchResult(MatchResult match)
    {
        Log.Info($"Match ended! Start={match.Start:yyyy-MM-dd HH:mm:ss}, End={match.End:yyyy-MM-dd HH:mm:ss}, Win={match.Win}, LocalSteamId={match.LocalSteamId}");

        foreach (TeamPoints tp in match.TeamPoints)
            Log.Info($"{tp.Team}: {tp.Points} points");
    }

    private static bool TryGetPlayerSteamId(Player player, out ulong steamId)
    {
        var id = player.GetModPlayer<AuthenticatedPlayer>().SteamId;
        if (id.HasValue)
        {
            steamId = id.Value;
            return true;
        }

        steamId = 0;
        return false;
    }
}