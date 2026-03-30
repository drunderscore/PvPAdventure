//using PvPAdventure.Common.MainMenu.API;
//using PvPAdventure.Common.Statistics;
//using Steamworks;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Terraria;
//using Terraria.Enums;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MainMenu.MatchHistory.Net;

//public static class SaveMatchNetHandler
//{
//    public static void HandlePacket(BinaryReader reader, int whoAmI)
//    {
//        DateTime matchStart = DateTime.FromBinary(reader.ReadInt64());
//        DateTime matchEnd = DateTime.FromBinary(reader.ReadInt64());

//        var pointsManager = ModContent.GetInstance<PointsManager>();
//        Player localPlayer = Main.LocalPlayer;
//        ulong localSteamId = SteamAuthSystem.ClientSteamId?.m_SteamID ?? 0;

//        if (!SteamAuthSystem.HasTicket)
//        {
//            Log.Warn("[SaveMatchNetHandler] Skipping match save because Steam auth ticket is not ready.");
//            return;
//        }

//        bool localWin = DetermineLocalWin(pointsManager, localPlayer);
//        TeamPoints[] teamPoints = BuildTeamPointsArray(pointsManager);
//        PlayerKD[] players = BuildPlayerKDArray(localSteamId);
//        TeamBossCompletion[] bosses = BuildBossCompletionArray(pointsManager);

//        var matchResult = new MatchResult(matchStart, matchEnd, localWin, localSteamId, teamPoints, players, bosses);

//        LogMatchResult(matchResult);
//        _ = SaveMatchSafeAsync(matchResult);
//    }

//    private static async Task SaveMatchSafeAsync(MatchResult matchResult)
//    {
//        try
//        {
//            ApiResult<bool> result = await MatchApi.SaveMatchAsync(matchResult).ConfigureAwait(false);

//            if (!result.IsSuccess)
//                Log.Error($"[SaveMatchNetHandler] Failed to save match. Status={(int)result.Status}, Error={result.ErrorMessage}");
//        }
//        catch (Exception ex)
//        {
//            Log.Error($"[SaveMatchNetHandler] Unexpected error while saving match: {ex}");
//        }
//    }

//    private static bool DetermineLocalWin(PointsManager pointsManager, Player localPlayer)
//    {
//        Team localTeam = (Team)localPlayer.team;
//        if (localTeam == Team.None)
//            return false;

//        List<KeyValuePair<Team, int>> scoredTeams = pointsManager.Points
//            .Where(x => x.Key != Team.None && x.Value > 0)
//            .OrderByDescending(x => x.Value)
//            .ToList();

//        if (scoredTeams.Count == 0)
//            return false;

//        int maxPoints = scoredTeams[0].Value;
//        int localTeamPoints = pointsManager.Points.TryGetValue(localTeam, out int points) ? points : 0;

//        return localTeamPoints == maxPoints && maxPoints > 0;
//    }

//    private static TeamPoints[] BuildTeamPointsArray(PointsManager pointsManager)
//    {
//        List<TeamPoints> result = [];

//        foreach ((Team team, int points) in pointsManager.Points)
//        {
//            if (team == Team.None)
//                continue;

//            result.Add(new TeamPoints(team, points));
//        }

//        return [.. result];
//    }

//    private static PlayerKD[] BuildPlayerKDArray(ulong localSteamId)
//    {
//        List<PlayerKD> result = [];

//        foreach (Player player in Main.ActivePlayers)
//        {
//            var statsPlayer = player.GetModPlayer<StatisticsPlayer>();
//            Team team = (Team)player.team;
//            ulong steamId = player.whoAmI == Main.myPlayer ? localSteamId : 0;

//            result.Add(new PlayerKD(team, steamId, player.name, statsPlayer.Kills, statsPlayer.Deaths));
//        }

//        return [.. result];
//    }

//    private static TeamBossCompletion[] BuildBossCompletionArray(PointsManager pointsManager)
//    {
//        List<TeamBossCompletion> result = [];

//        foreach ((Team team, ISet<short> downedNpcs) in pointsManager.DownedNpcs)
//            {
//            if (team == Team.None)
//                continue;

//            foreach (short bossId in downedNpcs)
//                result.Add(new TeamBossCompletion(bossId, team));
//        }

//        return [.. result];
//    }

//    private static void LogMatchResult(MatchResult match)
//    {
//        Log.Info($"Match ended! Start={match.Start:yyyy-MM-dd HH:mm:ss}, End={match.End:yyyy-MM-dd HH:mm:ss}, Win={match.Win}, LocalSteamId={match.LocalSteamId}");

//        foreach (TeamPoints tp in match.TeamPoints)
//            Log.Info($"{tp.Team}: {tp.Points} points");
//    }
//}