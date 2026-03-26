//using PvPAdventure.Common.MainMenu.MatchHistory;
//using PvPAdventure.Common.MainMenu.Profile;
//using PvPAdventure.Common.MainMenu.Shop;
//using PvPAdventure.Common.Statistics;
//using Steamworks;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Terraria;
//using Terraria.Enums;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MainMenu.DeprecatedClientStorage.Net;

//public static class SaveMatchNetHandler
//{
//    public static void HandlePacket(BinaryReader reader, int whoAmI)
//    {
//        DateTime matchStart = DateTime.FromBinary(reader.ReadInt64());
//        DateTime matchEnd = DateTime.FromBinary(reader.ReadInt64());

//        var pointsManager = ModContent.GetInstance<PointsManager>();
//        var localPlayer = Main.LocalPlayer;
//        ulong localSteamId = GetLocalSteamId();

//        bool localWin = DetermineLocalWin(pointsManager, localPlayer);

//        TeamPoints[] teamPoints = BuildTeamPointsArray(pointsManager);
//        PlayerKD[] players = BuildPlayerKDArray();
//        TeamBossCompletion[] bosses = BuildBossCompletionArray(pointsManager);

//        var matchResult = new MatchResult(matchStart, matchEnd, localWin, localSteamId, teamPoints, players, bosses);

//        LogMatchResult(matchResult);

//        MatchStorage.RecordAndSave(matchResult);
//        ProfileStorage.ApplyMatch(matchResult);
//    }

//    private static ulong GetLocalSteamId() => SteamUser.GetSteamID().m_SteamID;

//    private static bool DetermineLocalWin(PointsManager pointsManager, Player localPlayer)
//    {
//        var localTeam = (Team)localPlayer.team;
//        if (localTeam == Team.None)
//            return false;

//        var scoredTeams = pointsManager.Points
//            .Where(kvp => kvp.Key != Team.None && kvp.Value > 0)
//            .OrderByDescending(kvp => kvp.Value)
//            .ToList();

//        if (scoredTeams.Count == 0)
//            return false;

//        int maxPoints = scoredTeams[0].Value;
//        int localTeamPoints = pointsManager.Points.TryGetValue(localTeam, out var pts) ? pts : 0;

//        return localTeamPoints == maxPoints && maxPoints > 0;
//    }

//    private static TeamPoints[] BuildTeamPointsArray(PointsManager pointsManager)
//    {
//        var result = new List<TeamPoints>();

//        foreach (var (team, points) in pointsManager.Points)
//        {
//            if (team == Team.None)
//                continue;

//            result.Add(new TeamPoints(team, points));
//        }

//        return result.ToArray();
//    }

//    private static PlayerKD[] BuildPlayerKDArray()
//    {
//        var result = new List<PlayerKD>();
//        ulong localSteamId = GetLocalSteamId();

//        foreach (var player in Main.ActivePlayers)
//        {
//            var statsPlayer = player.GetModPlayer<StatisticsPlayer>();
//            var team = (Team)player.team;

//            ulong steamId = 0;
//            if (player.whoAmI == Main.myPlayer)
//                steamId = localSteamId;

//            result.Add(new PlayerKD(team, steamId, player.name, statsPlayer.Kills, statsPlayer.Deaths));
//        }

//        return result.ToArray();
//    }

//    private static TeamBossCompletion[] BuildBossCompletionArray(PointsManager pointsManager)
//    {
//        var result = new List<TeamBossCompletion>();

//        foreach (var (team, downedNpcs) in pointsManager.DownedNpcs)
//        {
//            if (team == Team.None)
//                continue;

//            foreach (var bossId in downedNpcs)
//                result.Add(new TeamBossCompletion(bossId, team));
//        }

//        return result.ToArray();
//    }

//    private static void LogMatchResult(MatchResult match)
//    {
//        Log.Info($" Match ended! Start={match.Start:yyyy-MM-dd HH:mm:ss}, End={match.End:yyyy-MM-dd HH:mm:ss}, Win={match.Win}, LocalSteamId={match.LocalSteamId}");

//        Log.Info("--- Team Points:---");
//        foreach (var tp in match.TeamPoints)
//            Log.Info($" {tp.Team}: {tp.Points} points");

//        Log.Info("--- Player K/D:---");
//        foreach (var player in match.Players)
//        {
//            string marker = player.SteamId == match.LocalSteamId ? " (YOU)" : "";
//            Log.Info($"  [{player.Team}] {player.PlayerName} ({player.SteamId}): {player.Kills}K / {player.Deaths}D{marker}");
//        }

//        Log.Info(" --- Boss Completions: ---");
//        if (match.BossScoreboard.Length == 0)
//        {
//            Log.Info("   (none)");
//            return;
//        }

//        foreach (var boss in match.BossScoreboard)
//            Log.Info($"  BossId={boss.BossId}, BossName={GetBossName(boss.BossId)} Team={boss.Team}");
//    }

//    public static string GetBossName(short bossId)
//    {
//        if (ContentSamples.NpcsByNetId.TryGetValue(bossId, out NPC npc))
//            return npc.FullName;

//        return $"Unknown Boss ({bossId})";
//    }
//}