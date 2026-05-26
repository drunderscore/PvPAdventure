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
                Log.Error($"[OfficialMatchReporter] Failed to post match. Status={(int)result.Status}, Error={result.ErrorMessage}");
                return;
            }

            if (result.Data == null)
            {
                WriteMatchPostConsole(WithRequestSummary("Match post succeeded, but the backend returned no match data.", result.RequestSummary));
                Log.Info("[OfficialMatchReporter] Posted match successfully, but received no data payload back.");
                return;
            }

            WriteMatchPostConsole(WithRequestSummary($"Match post succeeded. MatchId={result.Data.Id}", result.RequestSummary));
            Log.Info($"[OfficialMatchReporter] Posted match successfully. MatchId={result.Data.Id}");
        }
        catch (Exception ex)
        {
            WriteMatchPostConsole($"Match post failed with an unexpected error: {ex.GetType().Name}: {ex.Message}");
            Log.Error($"[OfficialMatchReporter] Unexpected error while posting match: {ex}");
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

        var players = BuildPlayersDictionary();
        var teams = BuildTeamsList(pointsManager);
        var metrics = new Dictionary<string, string>(); // empty for now

        DateTime start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        DateTime end = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);

        ConstructorInfo payloadConstructor = GetMatchPayloadConstructor(6);
        if (payloadConstructor != null)
        {
            return (MatchApi.MatchPayload)payloadConstructor.Invoke([start, end, "PvPAdventure", players, metrics, teams]);
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

    private static Dictionary<ulong, MatchApi.MatchPlayerPayload> BuildPlayersDictionary()
    {
        var result = new Dictionary<ulong, MatchApi.MatchPlayerPayload>();

        foreach (Player player in Main.ActivePlayers)
        {
            StatisticsPlayer statsPlayer = player.GetModPlayer<StatisticsPlayer>();
            int team = player.team;

            // Skip players without a valid SteamID to prevent dictionary key collisions
            if (!TryGetPlayerSteamId(player, out ulong steamId) || steamId == 0)
                continue;

            result[steamId] = new MatchApi.MatchPlayerPayload(
                Name: player.name,
                Team: team,
                Reward: 0, 
                Kills: statsPlayer.Kills,
                Deaths: statsPlayer.Deaths
            );
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
        Log.Info($"Match ended! Start={payload.Start:yyyy-MM-dd HH:mm:ss}, End={payload.End:yyyy-MM-dd HH:mm:ss}");

        for (int i = 0; i < payload.Teams.Count; i++)
        {
            var teamInfo = payload.Teams[i];
            if (teamInfo != null)
            {
                Log.Info($"Team {i}: {teamInfo.Value.Points} points");
            }
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
}
