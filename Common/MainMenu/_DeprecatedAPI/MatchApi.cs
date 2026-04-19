//using PvPAdventure.Common.MainMenu.MatchHistory;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Threading;
//using System.Threading.Tasks;
//using PvPAdventure.Common.Authentication;
//using Terraria.Enums;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MainMenu.API;

//internal static class MatchApi
//{
//    private static readonly JsonSerializerOptions JsonOptions = new()
//    {
//        PropertyNameCaseInsensitive = true
//    };

//    public static Task<ApiResult<List<MatchResult>>> GetMatchesAsync(CancellationToken cancellationToken = default)
//    {
//        return GetMatchesAsync(steamId: null, cancellationToken);
//    }

//    public static async Task<ApiResult<List<MatchResult>>> GetMatchesAsync(string? steamId, CancellationToken cancellationToken = default)
//    {
//        // Updated URIs to use "match/v1"
//        string uri = string.IsNullOrWhiteSpace(steamId)
//            ? "match/v1"
//            : $"match/v1?id={Uri.EscapeDataString(steamId)}";

//        ApiResult<string> result = await ApiClient.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);

//        if (!result.IsSuccess)
//            return ApiResult<List<MatchResult>>.Error(result.Status, result.ErrorMessage ?? "Failed to load matches.");

//        if (result.Status == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(result.Data))
//            return ApiResult<List<MatchResult>>.Success([], result.Status);

//        List<MatchPayload>? payloads;
//        try
//        {
//            payloads = JsonSerializer.Deserialize<List<MatchPayload>>(result.Data, JsonOptions);
//        }
//        catch (JsonException ex)
//        {
//            // Updated exception log
//            return ApiResult<List<MatchResult>>.Exception(ex, "Invalid JSON returned from 'match/v1'.");
//        }

//        if (payloads is null || payloads.Count == 0)
//            return ApiResult<List<MatchResult>>.Success([], result.Status);

//        ulong localSteamIdValue = 0;
//        var localSteamId = SteamAuthentication.ClientSteamId;
//        if (localSteamId.IsValid())
//            localSteamIdValue = localSteamId.m_SteamID;

//        List<MatchResult> matches = [];

//        for (int i = 0; i < payloads.Count; i++)
//            matches.Add(ToMatchResult(payloads[i], localSteamIdValue));

//        return ApiResult<List<MatchResult>>.Success(matches, result.Status);
//    }

//    public static async Task<ApiResult<string>> PostOfficialMatchAsync(MatchResult match, CancellationToken cancellationToken = default)
//    {
//        MatchPayload payload = FromMatchResult(match);
//        ApiResult<string> result = await ApiClient.PostStringAsync("match/v1", payload, cancellationToken).ConfigureAwait(false);

//        if (!result.IsSuccess)
//            return ApiResult<string>.Error(result.Status, result.ErrorMessage ?? "Failed to save match.");

//        string? matchId = TryExtractMatchId(result.Data);
//        if (string.IsNullOrWhiteSpace(matchId))
//            return ApiResult<string>.Error(result.Status, "Match POST succeeded but the backend did not return a match ID.");

//        return ApiResult<string>.Success(matchId, result.Status);
//    }

//    private static string? TryExtractMatchId(string? responseBody)
//    {
//        if (string.IsNullOrWhiteSpace(responseBody))
//            return null;

//        string trimmed = responseBody.Trim();

//        try
//        {
//            MatchCreateResponse? response = JsonSerializer.Deserialize<MatchCreateResponse>(trimmed, JsonOptions);
//            if (!string.IsNullOrWhiteSpace(response?.Id))
//                return response.Id;
//        }
//        catch (JsonException)
//        {
//        }

//        try
//        {
//            string? rawString = JsonSerializer.Deserialize<string>(trimmed, JsonOptions);
//            if (!string.IsNullOrWhiteSpace(rawString))
//                return rawString;
//        }
//        catch (JsonException)
//        {
//        }

//        return trimmed;
//    }

//    private static MatchResult ToMatchResult(MatchPayload payload, ulong localSteamId)
//    {
//        PlayerKD[] players = payload.Players
//            .Select(x => new PlayerKD(
//                team: (Team)x.Value.Team,
//                steamId: ulong.TryParse(x.Key, out ulong parsedSteamId) ? parsedSteamId : 0,
//                playerName: x.Value.Name,
//                kills: x.Value.Kills,
//                deaths: x.Value.Deaths))
//            .ToArray();

//        List<TeamPoints> teamPoints = [];
//        List<TeamBossCompletion> bossScoreboard = [];

//        for (int teamIndex = 0; teamIndex < payload.Teams.Count; teamIndex++)
//        {
//            MatchTeamPayload? team = payload.Teams[teamIndex];
//            if (team is null || (Team)teamIndex == Team.None)
//                continue;

//            teamPoints.Add(new TeamPoints((Team)teamIndex, team.Points));

//            for (int i = 0; i < team.Bosses.Count; i++)
//                bossScoreboard.Add(new TeamBossCompletion(team.Bosses[i], (Team)teamIndex));
//        }

//        bool win = false;
//        string localSteamIdText = localSteamId.ToString();

//        if (payload.Players.TryGetValue(localSteamIdText, out MatchPlayerPayload? localPlayer))
//        {
//            int maxPoints = teamPoints.Count > 0 ? teamPoints.Max(x => x.Points) : 0;
//            int localTeamIndex = localPlayer.Team;
//            int localPoints = 0;

//            if (localTeamIndex >= 0 && localTeamIndex < payload.Teams.Count && payload.Teams[localTeamIndex] is not null)
//                localPoints = payload.Teams[localTeamIndex]!.Points;

//            win = localTeamIndex != (int)Team.None && localPoints == maxPoints && maxPoints > 0;
//        }

//        return new MatchResult(
//            start: payload.Start,
//            end: payload.End,
//            win: win,
//            localSteamId: localSteamId,
//            teamPoints: [.. teamPoints],
//            players: players,
//            bossScoreboard: [.. bossScoreboard]);
//    }

//    private static MatchPayload FromMatchResult(MatchResult match)
//    {
//        Dictionary<string, MatchPlayerPayload> players = [];

//        if (match.Players != null)
//        {
//            for (int i = 0; i < match.Players.Length; i++)
//            {
//                PlayerKD player = match.Players[i];
//                string key = player.SteamId != 0 ? player.SteamId.ToString() : $"player:{i}";

//                players[key] = new MatchPlayerPayload
//                {
//                    Name = player.PlayerName,
//                    Team = (int)player.Team,
//                    Kills = player.Kills,
//                    Deaths = player.Deaths
//                };
//            }
//        }

//        int teamCount = GetSerializedTeamCount();
//        List<MatchTeamPayload?> teams = [];
//        for (int i = 0; i < teamCount; i++)
//            teams.Add(null);

//        if (match.TeamPoints != null)
//        {
//            for (int i = 0; i < match.TeamPoints.Length; i++)
//            {
//                TeamPoints tp = match.TeamPoints[i];
//                int teamIndex = (int)tp.Team;

//                if (teamIndex < 0 || teamIndex >= teams.Count || tp.Team == Team.None)
//                    continue;

//                teams[teamIndex] ??= new MatchTeamPayload();
//                teams[teamIndex]!.Points = tp.Points;
//            }
//        }

//        if (match.BossScoreboard != null)
//        {
//            for (int i = 0; i < match.BossScoreboard.Length; i++)
//            {
//                TeamBossCompletion boss = match.BossScoreboard[i];
//                int teamIndex = (int)boss.Team;

//                if (teamIndex < 0 || teamIndex >= teams.Count || boss.Team == Team.None)
//                    continue;

//                teams[teamIndex] ??= new MatchTeamPayload();
//                teams[teamIndex]!.Bosses.Add(boss.BossId);
//            }
//        }

//        return new MatchPayload
//        {
//            Start = match.Start.ToUniversalTime(),
//            End = match.End.ToUniversalTime(),
//            Players = players,
//            Teams = teams
//        };
//    }

//    private static int GetSerializedTeamCount()
//    {
//        Team[] values = (Team[])Enum.GetValues(typeof(Team));
//        int max = 0;

//        for (int i = 0; i < values.Length; i++)
//        {
//            int value = (int)values[i];
//            if (value > max)
//                max = value;
//        }

//        return max + 1;
//    }

//    private sealed class MatchCreateResponse
//    {
//        [JsonPropertyName("id")]
//        public string Id { get; set; } = "";
//    }

//    private sealed class MatchPayload
//    {
//        [JsonPropertyName("start")]
//        public DateTime Start { get; set; }

//        [JsonPropertyName("end")]
//        public DateTime End { get; set; }

//        [JsonPropertyName("players")]
//        public Dictionary<string, MatchPlayerPayload> Players { get; set; } = [];

//        [JsonPropertyName("teams")]
//        public List<MatchTeamPayload?> Teams { get; set; } = [];
//    }

//    private sealed class MatchPlayerPayload
//    {
//        [JsonPropertyName("name")]
//        public string Name { get; set; } = "";

//        [JsonPropertyName("team")]
//        public int Team { get; set; }

//        [JsonPropertyName("kills")]
//        public int Kills { get; set; }

//        [JsonPropertyName("deaths")]
//        public int Deaths { get; set; }
//    }

//    private sealed class MatchTeamPayload
//    {
//        [JsonPropertyName("points")]
//        public int Points { get; set; }

//        [JsonPropertyName("bosses")]
//        public List<short> Bosses { get; set; } = [];
//    }
//}