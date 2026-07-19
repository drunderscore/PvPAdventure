using PvPAdventure.Common.Statistics;
using PvPFramework.Common.Scoreboard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.GameReporters;

internal static class MatchReporter
{
    private const string GameMode = "pvpa";

    public static void PostCompletedMatchSafe(DateTime startUtc, DateTime endUtc)
    {
        if (PvPHubCompat.IsPvPHubLoaded)
            ExecutePost(startUtc, endUtc, null);
    }

    public static void PostCompletedMatchSafe(DateTime startUtc, DateTime endUtc, string replayFilePath)
    {
        if (!PvPHubCompat.IsPvPHubLoaded)
            return;

        if (string.IsNullOrWhiteSpace(replayFilePath) || !File.Exists(replayFilePath))
        {
            Log.Chat($"Replay file missing. Falling back to match/v1. Path={replayFilePath}");
            replayFilePath = null;
        }

        ExecutePost(startUtc, endUtc, replayFilePath);
    }

    private static void ExecutePost(DateTime startUtc, DateTime endUtc, string replayFilePath)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        MatchPayload payload = BuildMatchPayload(startUtc, endUtc);
        LogMatchPayload(payload);

        if (!IsValidPayload(payload))
            return;

        string payloadJson = JsonSerializer.Serialize(payload, PvPHubCompat.JsonOptions);
        _ = PostMatchSafeAsync(payloadJson, replayFilePath);
    }

    private static async Task PostMatchSafeAsync(string payloadJson, string replayFilePath)
    {
        try
        {
            PvPHubCallResult result = await PvPHubCompat.PostMatchAsync(payloadJson, replayFilePath)
                .ConfigureAwait(false);
            string version = string.IsNullOrWhiteSpace(replayFilePath) ? "v1" : "v2";

            if (!result.Success)
            {
                string message = result.StatusCode == 401
                    ? $"Match {version} post failed: 401 Unauthorized. Error={result.Error}"
                    : $"Match {version} post failed: Status={result.StatusCode}. Error={result.Error}";
                WriteMatchPostConsole(WithRequestSummary(message, result.RequestSummary));
                Log.Error(message);
                return;
            }

            if (!result.TryGetDataInt64("id", out long matchId))
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
            Log.Error($"Unexpected error while posting match: {ex}");
        }
    }

    private static MatchPayload BuildMatchPayload(DateTime startUtc, DateTime endUtc)
    {
        PointsManager pointsManager = ModContent.GetInstance<PointsManager>();
        PvPHubCompat.LogMatchPostAuthPreflight();

        Dictionary<ulong, MatchPlayerPayload> players = BuildPlayersDictionary(pointsManager);
        if (players.Count == 0)
            Log.Chat("Refusing to post match because payload has no authenticated players.");

        return new MatchPayload(
            DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
            GameMode,
            players,
            new Dictionary<string, string>(),
            BuildTeamsList(pointsManager));
    }

    private static Dictionary<ulong, MatchPlayerPayload> BuildPlayersDictionary(PointsManager pointsManager)
    {
        Dictionary<ulong, MatchPlayerPayload> result = [];
        bool hasWinningTeam = TryGetSingleWinningTeam(pointsManager, out Team winningTeam);

        foreach (Player player in Main.ActivePlayers)
        {
            if (player?.active != true)
                continue;

            ScoreboardEntry statsPlayer = ScoreboardService.GetPlayerStats(player);
            if (!PvPHubCompat.TryGetSteamId(player, out ulong steamId))
            {
                Log.Chat($"Skipping player with no valid SteamID. PlayerName={player.name}");
                continue;
            }

            MatchRewardContext rewardContext = MatchRewardCalculator.CreateContext(player, pointsManager);
            uint reward = MatchRewardCalculator.Calculate(rewardContext);
            Dictionary<string, uint> stats = StatsReporter.CopyStats(player);
            Dictionary<string, IDictionary<int, uint>> itemStats = StatsReporter.CopyItemStats(player);
            bool winner = hasWinningTeam && rewardContext.Team == winningTeam;

            result[steamId] = new MatchPlayerPayload(
                player.name,
                (uint)rewardContext.Team,
                reward,
                statsPlayer.Kills,
                statsPlayer.Deaths,
                winner,
                stats,
                itemStats);

            Log.Info($"Reward for {player.name}: Team={rewardContext.Team}, Winner={winner}, TeamPoints={rewardContext.TeamPoints}, Kills={rewardContext.Kills}, Deaths={rewardContext.Deaths}, Reward={reward}, Stats={stats.Count}, ItemStats={itemStats.Count}");
        }

        return result;
    }

    private static bool TryGetSingleWinningTeam(PointsManager pointsManager, out Team winningTeam)
    {
        winningTeam = Team.None;
        if (pointsManager == null)
            return false;

        int winningPoints = int.MinValue;
        int winningTeamCount = 0;
        foreach ((Team team, int points) in pointsManager.Points)
        {
            if (team == Team.None || points <= 0)
                continue;

            if (points > winningPoints)
            {
                winningPoints = points;
                winningTeam = team;
                winningTeamCount = 1;
            }
            else if (points == winningPoints)
            {
                winningTeamCount++;
            }
        }

        return winningTeamCount == 1;
    }

    private static List<MatchTeamPayload?> BuildTeamsList(PointsManager pointsManager)
    {
        List<MatchTeamPayload?> result = [];
        for (int i = 0; i <= 6; i++)
            result.Add(null);

        foreach ((Team team, int points) in pointsManager.Points)
        {
            if (team == Team.None)
                continue;

            List<short> bosses = [];
            if (pointsManager.DownedNpcs.TryGetValue(team, out ISet<short> downedNpcs))
                bosses.AddRange(downedNpcs);

            int teamId = (int)team;
            while (result.Count <= teamId)
                result.Add(null);
            result[teamId] = new MatchTeamPayload(points, bosses);
        }

        return result;
    }

    private static void LogMatchPayload(MatchPayload payload)
    {
        Log.Chat($"Match ended! Start={payload.Start:yyyy-MM-dd HH:mm:ss}, End={payload.End:yyyy-MM-dd HH:mm:ss}");
        Log.Chat($"Payload players={payload.Players.Count}, teams={payload.Teams.Count}, team0Null={payload.Teams.Count > 0 && payload.Teams[0] == null}");

        for (int i = 0; i < payload.Teams.Count; i++)
            if (payload.Teams[i] is MatchTeamPayload team)
                Log.Info($"Team {i}: {team.Points} points");
    }

    private static bool IsValidPayload(MatchPayload payload)
    {
        if (payload.Players.Count == 0)
        {
            Log.Chat("Refusing to post malformed match: no players in payload.");
            return false;
        }

        if (payload.Teams.Count == 0 || payload.Teams[0] != null)
        {
            Log.Chat("Refusing to post malformed match: team 0 must exist and be null.");
            return false;
        }

        return true;
    }

    private static string WithRequestSummary(string message, string requestSummary) =>
        string.IsNullOrWhiteSpace(requestSummary) ? message : $"{message} ({requestSummary})";

    private static void WriteMatchPostConsole(string message) =>
        Console.WriteLine($"[PvPAdventure/OfficialMatchReporter] {message}");

    private sealed record MatchPayload(
        DateTime Start,
        DateTime End,
        string GameMode,
        IDictionary<ulong, MatchPlayerPayload> Players,
        IDictionary<string, string> Metrics,
        List<MatchTeamPayload?> Teams);

    private readonly record struct MatchPlayerPayload(
        string Name,
        uint Team,
        uint Reward,
        int Kills,
        int Deaths,
        bool Winner,
        IDictionary<string, uint> Stats,
        IDictionary<string, IDictionary<int, uint>> ItemStats);

    private readonly record struct MatchTeamPayload(int Points, IList<short> Bosses);
}
