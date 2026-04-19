using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.MatchHistory;
using System.Collections.Generic;
using Terraria.Enums;

namespace PvPAdventure.Common.MainMenu.PlayerStats;

public static class PlayerStatsContentBuilder
{
    public static PlayerStatsSummary FromApi(ApiPlayerStatsResponse response)
    {
        if (response == null)
            return default;

        return new PlayerStatsSummary
        {
            Kills = response.Kills,
            Deaths = response.Deaths,
            Wins = response.Wins,
            Losses = response.Losses,
            TeamPointsTotal = response.TeamPointsTotal
        };
    }

    public static PlayerStatsSummary FromMatches(IReadOnlyList<MatchResult> matches, ulong steamUserId)
    {
        int kills = 0;
        int deaths = 0;
        int wins = 0;
        int losses = 0;
        int teamPointsTotal = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            MatchResult match = matches[i];

            if (match.Win)
                wins++;
            else
                losses++;

            ulong myId = steamUserId;
            if (myId == 0)
                myId = (ulong)match.LocalSteamId;

            if (myId == 0)
                continue;

            Team myTeam = Team.None;

            PlayerKD[] players = match.Players ?? [];
            for (int j = 0; j < players.Length; j++)
            {
                if ((ulong)players[j].SteamId != myId)
                    continue;

                kills += players[j].Kills;
                deaths += players[j].Deaths;
                myTeam = players[j].Team;
                break;
            }

            if (myTeam == Team.None)
                continue;

            TeamPoints[] teamPoints = match.TeamPoints ?? [];
            for (int j = 0; j < teamPoints.Length; j++)
            {
                if (teamPoints[j].Team != myTeam)
                    continue;

                teamPointsTotal += teamPoints[j].Points;
                break;
            }
        }

        return new PlayerStatsSummary
        {
            Kills = kills,
            Deaths = deaths,
            Wins = wins,
            Losses = losses,
            TeamPointsTotal = teamPointsTotal
        };
    }
}
