using PvPHub.Common.MainMenu.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using CompletedMatchPayload = PvPHub.Common.MainMenu.API.MatchHistory.MatchApi.CompletedMatchPayload;
using MatchPayload = PvPHub.Common.MainMenu.API.MatchHistory.MatchApi.MatchPayload;
using MatchPlayerPayload = PvPHub.Common.MainMenu.API.MatchHistory.MatchApi.MatchPlayerPayload;
using MatchTeamPayload = PvPHub.Common.MainMenu.API.MatchHistory.MatchApi.MatchTeamPayload;

namespace PvPAdventure.Common.Game.GameReporters;

internal static class MatchReporter
{
    private const string GameMode = "pvpa";
    private const string MatchTokenMetric = "match_token";

    public static void PostCompletedMatchSafe(CompletedAdventureMatch completedMatch, string replayFilePath = null)
    {
        if (Main.netMode != NetmodeID.Server || completedMatch == null)
            return;

        if (!string.IsNullOrWhiteSpace(replayFilePath) && !File.Exists(replayFilePath))
        {
            Log.Warn($"Replay file missing. Falling back to match/v1. Path={replayFilePath}");
            replayFilePath = null;
        }

        try
        {
            MatchPayload payload = BuildMatchPayload(completedMatch);
            LogMatchPayload(payload, completedMatch.Token);

            if (!IsValidPayload(payload))
                return;

            // Intentionally no automatic retry: Tavernkeep currently has no idempotency key, so an
            // ambiguous retry could duplicate the match, achievements, and gem rewards.
            _ = PostMatchSafeAsync(payload, replayFilePath);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to build or queue match payload. MatchToken={completedMatch.Token}, Error={ex}");
        }
    }

    private static async Task PostMatchSafeAsync(MatchPayload payload, string replayFilePath)
    {
        try
        {
            ApiResult<CompletedMatchPayload> result = await PvPHubService.PostMatchAsync(payload, replayFilePath)
                .ConfigureAwait(false);
            string version = string.IsNullOrWhiteSpace(replayFilePath) ? "v1" : "v2";

            if (!result.IsSuccess)
            {
                int statusCode = (int)result.Status;
                string message = statusCode == 401
                    ? $"Match {version} post failed: 401 Unauthorized. Error={result.ErrorMessage}"
                    : $"Match {version} post failed: Status={statusCode}. Error={result.ErrorMessage}";
                WriteMatchPostConsole(WithRequestSummary(message, result.RequestSummary));
                Log.Error(message + " The request was not retried because match posting is not idempotent.");
                return;
            }

            long matchId = result.Data?.Id ?? 0;
            if (matchId <= 0)
            {
                WriteMatchPostConsole(WithRequestSummary(
                    $"Match {version} post succeeded, but the backend returned no match id.", result.RequestSummary));
                return;
            }

            WriteMatchPostConsole(WithRequestSummary(
                $"Match {version} post succeeded. MatchId={matchId}", result.RequestSummary));
            Log.Info($"Posted match successfully. MatchId={matchId}");
        }
        catch (Exception ex)
        {
            WriteMatchPostConsole($"Match post failed with an unexpected error: {ex.GetType().Name}: {ex.Message}");
            Log.Error($"Unexpected error while posting match; request was not retried: {ex}");
        }
    }

    private static MatchPayload BuildMatchPayload(CompletedAdventureMatch completedMatch)
    {
        PvPHubService.LogMatchPostAuthPreflight();

        Dictionary<ulong, MatchPlayerPayload> players = [];
        foreach ((ulong steamId, CompletedAdventurePlayer player) in completedMatch.Players)
        {
            Dictionary<string, uint> stats = new(player.Stats);
            Dictionary<string, IDictionary<int, uint>> itemStats = [];
            foreach ((string statKey, IDictionary<int, uint> byItem) in player.ItemStats)
                itemStats[statKey] = new Dictionary<int, uint>(byItem);

            players[steamId] = new MatchPlayerPayload(
                player.Name,
                (uint)player.Team,
                player.Reward,
                player.Kills,
                player.Deaths,
                player.Winner,
                stats,
                itemStats);

            Log.Info($"Match player: Name={player.Name}, SteamId={steamId}, Team={player.Team}, " +
                     $"Winner={player.Winner}, Kills={player.Kills}, Deaths={player.Deaths}, " +
                     $"Reward={player.Reward}, Stats={stats.Count}, ItemStats={itemStats.Count}");
        }

        Dictionary<string, string> metrics = new()
        {
            [MatchTokenMetric] = completedMatch.Token
        };

        return new MatchPayload(
            DateTime.SpecifyKind(completedMatch.StartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(completedMatch.EndUtc, DateTimeKind.Utc),
            GameMode,
            players,
            metrics,
            BuildTeamsList(completedMatch.Teams));
    }

    private static List<MatchTeamPayload?> BuildTeamsList(
        IReadOnlyDictionary<Team, CompletedAdventureTeam> teams)
    {
        int lastTeamId = Math.Max(0, teams.Keys.Select(team => (int)team).DefaultIfEmpty(0).Max());
        List<MatchTeamPayload?> result = Enumerable.Repeat<MatchTeamPayload?>(null, lastTeamId + 1).ToList();

        foreach ((Team team, CompletedAdventureTeam teamResult) in teams)
        {
            if (team == Team.None)
                continue;

            int teamId = (int)team;
            while (result.Count <= teamId)
                result.Add(null);
            result[teamId] = new MatchTeamPayload(teamResult.Points, teamResult.Bosses.ToList());
        }

        return result;
    }

    private static void LogMatchPayload(MatchPayload payload, string matchToken)
    {
        Log.Chat($"Match ended! Token={matchToken}, Start={payload.Start:O}, End={payload.End:O}");
        Log.Chat($"Payload players={payload.Players.Count}, teams={payload.Teams.Count}, " +
                 $"team0Null={payload.Teams.Count > 0 && payload.Teams[0] == null}");

        for (int i = 0; i < payload.Teams.Count; i++)
            if (payload.Teams[i] is MatchTeamPayload team)
                Log.Info($"Team {i}: {team.Points} points, {team.Bosses.Count} bosses");
    }

    private static bool IsValidPayload(MatchPayload payload)
    {
        if (payload.End < payload.Start)
        {
            Log.Error("Refusing to post malformed match: end time precedes start time.");
            return false;
        }

        if (payload.Players.Count == 0)
        {
            Log.Warn("Refusing to post malformed match: no authenticated participants in payload.");
            return false;
        }

        if (payload.Teams.Count == 0 || payload.Teams[0] != null)
        {
            Log.Error("Refusing to post malformed match: team 0 must exist and be null.");
            return false;
        }

        foreach ((ulong steamId, MatchPlayerPayload player) in payload.Players)
        {
            if (steamId == 0 || player.Team == 0 || player.Team >= payload.Teams.Count ||
                payload.Teams[(int)player.Team] == null || player.Kills < 0 || player.Deaths < 0)
            {
                Log.Error($"Refusing to post malformed match player. SteamId={steamId}, Team={player.Team}, " +
                          $"Kills={player.Kills}, Deaths={player.Deaths}");
                return false;
            }
        }

        return true;
    }

    private static string WithRequestSummary(string message, string requestSummary) =>
        string.IsNullOrWhiteSpace(requestSummary) ? message : $"{message} ({requestSummary})";

    private static void WriteMatchPostConsole(string message) =>
        Console.WriteLine($"[PvPAdventure/OfficialMatchReporter] {message}");
}
