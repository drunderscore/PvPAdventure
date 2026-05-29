using PvPAdventure.Common.Statistics;
using PvPHub.Common.Authentication;
using PvPHub.Common.MainMenu.API.MatchHistory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.GameTimer;

[JITWhenModsEnabled("PvPHub")]
[ExtendsFromMod("PvPHub")]
internal static class OfficialMatchReporter
{
    public static void PostCompletedMatchSafe(DateTime startUtc, DateTime endUtc)
    {
        if (!ModLoader.TryGetMod("PvPHub", out Mod _))
            return;

        ExecutePost(startUtc, endUtc);
    }

    private static void ExecutePost(DateTime startUtc, DateTime endUtc)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        MatchApi.MatchPayload payload = BuildMatchPayload(startUtc, endUtc);
        LogMatchPayload(payload);

        if (!IsValidPayload(payload))
            return;

        _ = PostMatchSafeAsync(payload);
    }

    private static async Task PostMatchSafeAsync(MatchApi.MatchPayload payload)
    {
        try
        {
            var result = await MatchApi.PostOfficialMatchAsync(payload).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                string consoleMessage = result.Status == HttpStatusCode.Unauthorized
                    ? $"Match post failed: 401 Unauthorized. The backend rejected the match post credentials. Error={result.ErrorMessage}"
                    : $"Match post failed: Status={(int)result.Status} {result.Status}. Error={result.ErrorMessage}";

                WriteMatchPostConsole(WithRequestSummary(consoleMessage, result.RequestSummary));
                Log.Error($"Failed to post match. Status={(int)result.Status}, Error={result.ErrorMessage}");
                return;
            }

            if (result.Data == null)
            {
                WriteMatchPostConsole(WithRequestSummary("Match post succeeded, but the backend returned no match data.", result.RequestSummary));
                Log.Info("Posted match successfully, but received no data payload back.");
                return;
            }

            WriteMatchPostConsole(WithRequestSummary($"Match post succeeded. MatchId={result.Data.Id}", result.RequestSummary));
            Log.Info($"Posted match successfully. MatchId={result.Data.Id}");
        }
        catch (Exception ex)
        {
            WriteMatchPostConsole($"Match post failed with an unexpected error: {ex.GetType().Name}: {ex.Message}");
            Log.Error($"Unexpected error while posting match: {ex}");
        }
    }

    private static string WithRequestSummary(string message, string requestSummary)
    {
        if (string.IsNullOrWhiteSpace(requestSummary))
            return message;

        return $"{message} ({requestSummary})";
    }

    private static void WriteMatchPostConsole(string message)
    {
        Console.WriteLine($"[PvPAdventure/OfficialMatchReporter] {message}");
    }

    private static MatchApi.MatchPayload BuildMatchPayload(DateTime startUtc, DateTime endUtc)
    {
        PointsManager pointsManager = ModContent.GetInstance<PointsManager>();

        var players = BuildPlayersDictionary(pointsManager);

        if (players.Count == 0)
            Log.Chat("Refusing to post match because payload has no authenticated players.");

        var teams = BuildTeamsList(pointsManager);
        var metrics = new Dictionary<string, string>(); // empty for now

        DateTime start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        DateTime end = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);

        ConstructorInfo payloadConstructor = GetMatchPayloadConstructor(6);
        if (payloadConstructor != null)
        {
            return (MatchApi.MatchPayload)payloadConstructor.Invoke([start, end, "pvpa", players, metrics, teams]);
        }

        payloadConstructor = GetMatchPayloadConstructor(5);
        if (payloadConstructor != null)
        {
            return (MatchApi.MatchPayload)payloadConstructor.Invoke([start, end, players, metrics, teams]);
        }

        throw new MissingMethodException(typeof(MatchApi.MatchPayload).FullName, ".ctor");
    }

    private static ConstructorInfo GetMatchPayloadConstructor(int parameterCount)
    {
        foreach (ConstructorInfo constructor in typeof(MatchApi.MatchPayload).GetConstructors())
        {
            if (constructor.GetParameters().Length == parameterCount)
            {
                return constructor;
            }
        }

        return null;
    }

    private static Dictionary<ulong, MatchApi.MatchPlayerPayload> BuildPlayersDictionary(PointsManager pointsManager)
    {
        Dictionary<ulong, MatchApi.MatchPlayerPayload> result = [];

        foreach (Player player in Main.ActivePlayers)
        {
            StatisticsPlayer statsPlayer = player.GetModPlayer<StatisticsPlayer>();

            // Skip players without a valid SteamID to prevent dictionary key collisions
            if (!TryGetPlayerSteamId(player, out ulong steamId) || steamId == 0)
            {
                Log.Chat($"Warning: Skipping player with no valid SteamID when building match payload. PlayerName={player.name}");
                continue;
            }

            if (steamId > long.MaxValue)
            {
                Log.Chat($"Skipping player with unsupported SteamID for match post. PlayerName={player.name}, SteamId={steamId}");
                continue;
            }

            MatchRewardContext rewardContext = MatchRewardCalculator.CreateContext(player, pointsManager);
            uint reward = MatchRewardCalculator.Calculate(rewardContext);

            result[steamId] = new MatchApi.MatchPlayerPayload(
                Name: player.name,
                Team: (uint)rewardContext.Team,
                Reward: reward,
                Kills: statsPlayer.Kills,
                Deaths: statsPlayer.Deaths);

            Log.Info($"Reward for {player.name}: Team={rewardContext.Team}, TeamPoints={rewardContext.TeamPoints}, Kills={rewardContext.Kills}, Deaths={rewardContext.Deaths}, Reward={reward}");
        }

        return result;
    }

    private static List<MatchApi.MatchTeamPayload?> BuildTeamsList(PointsManager pointsManager)
    {
        var result = new List<MatchApi.MatchTeamPayload?>();

        // Empty team results (6 teams)
        for (int i = 0; i <= 6; i++)
        {
            result.Add(null);
        }

        foreach ((Team team, int points) in pointsManager.Points)
        {
            if (team == Team.None)
                continue;

            int teamId = (int)team;

            var bossesList = new List<short>();
            if (pointsManager.DownedNpcs.TryGetValue(team, out ISet<short> downedNpcs))
            {
                bossesList.AddRange(downedNpcs);
            }

            while (result.Count <= teamId)
                result.Add(null);

            result[teamId] = new MatchApi.MatchTeamPayload(points, bossesList);
        }

        return result;
    }

    private static void LogMatchPayload(MatchApi.MatchPayload payload)
    {
        Log.Chat($"Match ended! Start={payload.Start:yyyy-MM-dd HH:mm:ss}, End={payload.End:yyyy-MM-dd HH:mm:ss}");
        Log.Chat($"Payload players={payload.Players?.Count ?? 0}, teams={payload.Teams?.Count ?? 0}, team0Null={payload.Teams != null && payload.Teams.Count > 0 && payload.Teams[0] == null}");

        for (int i = 0; i < payload.Teams.Count; i++)
        {
            var teamInfo = payload.Teams[i];
            if (teamInfo != null)
                Log.Info($"Team {i}: {teamInfo.Value.Points} points");
        }
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

    private static bool IsValidPayload(MatchApi.MatchPayload payload)
    {
        if (payload.Players == null || payload.Players.Count == 0)
        {
            Log.Chat("Refusing to post malformed match: no players in payload.");
            return false;
        }

        if (payload.Teams == null || payload.Teams.Count == 0 || payload.Teams[0] != null)
        {
            Log.Chat("Refusing to post malformed match: team 0 must exist and be null.");
            return false;
        }

        return true;
    }
}
