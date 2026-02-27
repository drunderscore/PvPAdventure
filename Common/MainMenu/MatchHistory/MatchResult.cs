using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Terraria.Enums;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

/// <summary>
/// Represents the outcome and details of a completed match, including timing, player statistics, team points, and boss
/// completion information.
/// </summary>
public readonly struct MatchResult
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public bool Win { get; init; }
    public ulong LocalSteamId { get; init; }
    public PlayerKD[] Players { get; init; }
    public TeamPoints[] TeamPoints { get; init; }
    public TeamBossCompletion[] BossScoreboard { get; init; }

    public MatchResult(DateTime start, DateTime end, bool win, ulong localSteamId,
        TeamPoints[] teamPoints, PlayerKD[] players, TeamBossCompletion[] bossScoreboard)
    {
        Start = start;
        End = end;
        Win = win;
        LocalSteamId = localSteamId;
        Players = players;
        TeamPoints = teamPoints;
        BossScoreboard = bossScoreboard;
    }

    public string ToDaysAgoText()
    {
        DateTime now = DateTime.Now;
        DateTime today = now.Date;
        DateTime day = Start.Date;

        if (day == today)
            return "Today, " + Start.ToString("h:mm tt", CultureInfo.InvariantCulture);

        if (day == today.AddDays(-1))
            return "Yesterday, " + Start.ToString("h:mm tt", CultureInfo.InvariantCulture);

        int daysAgo = (int)(today - day).TotalDays;

        if (daysAgo >= 2 && daysAgo <= 30)
            return $"{daysAgo} days ago, " + Start.ToString("h:mm tt", CultureInfo.InvariantCulture);

        return Start.ToString("d MMMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
    }

    public string ToDurationDetailsText()
    {
        TimeSpan d = End - Start;
        if (d < TimeSpan.Zero)
            d = d.Negate();

        int totalSeconds = (int)Math.Round(d.TotalSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
        {
            if (minutes > 0)
                return $"{hours} hour{(hours == 1 ? "" : "s")}, {minutes} minute{(minutes == 1 ? "" : "s")}";
            return $"{hours} hour{(hours == 1 ? "" : "s")}";
        }

        if (minutes >= 10)
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}";

        if (minutes > 0 && seconds > 0)
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}, {seconds} second{(seconds == 1 ? "" : "s")}";

        if (minutes > 0)
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}";

        return $"{seconds} second{(seconds == 1 ? "" : "s")}";
    }

    #region .nbt serialization
    public TagCompound ToTag()
    {
        List<TagCompound> players = [];
        if (Players != null)
        {
            for (int i = 0; i < Players.Length; i++)
                players.Add(Players[i].ToTag());
        }

        List<TagCompound> teamPoints = [];
        if (TeamPoints != null)
        {
            for (int i = 0; i < TeamPoints.Length; i++)
                teamPoints.Add(TeamPoints[i].ToTag());
        }

        List<TagCompound> bosses = [];
        if (BossScoreboard != null)
        {
            for (int i = 0; i < BossScoreboard.Length; i++)
                bosses.Add(BossScoreboard[i].ToTag());
        }

        return new TagCompound
        {
            ["V"] = 1,
            ["Start"] = Start.ToBinary(),
            ["End"] = End.ToBinary(),
            ["Win"] = Win,
            // store as long (SteamID64 fits in signed 64-bit range in practice)
            ["LocalSteamId"] = (long)LocalSteamId,
            ["Players"] = players,
            ["TeamPoints"] = teamPoints,
            ["BossScoreboard"] = bosses
        };
    }

    public static MatchResult FromTag(TagCompound tag)
    {
        DateTime start = DateTime.FromBinary(tag.GetLong("Start"));
        DateTime end = DateTime.FromBinary(tag.GetLong("End"));
        bool win = tag.GetBool("Win");

        ulong localSteamId = 0;
        if (tag.ContainsKey("LocalSteamId"))
            localSteamId = (ulong)tag.GetLong("LocalSteamId");

        PlayerKD[] players = [];
        if (tag.ContainsKey("Players"))
        {
            IList<TagCompound> list = tag.GetList<TagCompound>("Players");
            players = new PlayerKD[list.Count];

            for (int i = 0; i < list.Count; i++)
                players[i] = PlayerKD.FromTag(list[i]);
        }

        TeamPoints[] points = [];
        if (tag.ContainsKey("TeamPoints"))
        {
            IList<TagCompound> list = tag.GetList<TagCompound>("TeamPoints");
            points = new TeamPoints[list.Count];

            for (int i = 0; i < list.Count; i++)
                points[i] = MatchHistory.TeamPoints.FromTag(list[i]);
        }

        TeamBossCompletion[] bosses = [];
        if (tag.ContainsKey("BossScoreboard"))
        {
            IList<TagCompound> list = tag.GetList<TagCompound>("BossScoreboard");
            bosses = new TeamBossCompletion[list.Count];

            for (int i = 0; i < list.Count; i++)
                bosses[i] = TeamBossCompletion.FromTag(list[i]);
        }

        return new MatchResult(start, end, win, localSteamId, points, players, bosses);
    }
    #endregion
}

public readonly struct PlayerKD
{
    public Team Team { get; init; }
    public ulong SteamId { get; init; }
    public string PlayerName { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }

    public PlayerKD(Team team, ulong steamId, string playerName, int kills, int deaths)
    {
        Team = team;
        SteamId = steamId;
        PlayerName = playerName;
        Kills = kills;
        Deaths = deaths;
    }

    public TagCompound ToTag()
    {
        return new TagCompound
        {
            ["Team"] = (int)Team,
            ["SteamId"] = (long)SteamId,
            ["PlayerName"] = PlayerName ?? "",
            ["Kills"] = Kills,
            ["Deaths"] = Deaths
        };
    }

    public static PlayerKD FromTag(TagCompound tag)
    {
        Team team = (Team)tag.GetInt("Team");
        ulong steamId = (ulong)tag.GetLong("SteamId");

        string name = "";
        if (tag.TryGet("PlayerName", out string n) && n != null)
            name = n;

        int kills = tag.GetInt("Kills");
        int deaths = tag.GetInt("Deaths");

        return new PlayerKD(team, steamId, name, kills, deaths);
    }
}

public readonly struct TeamPoints
{
    public Team Team { get; init; }
    public int Points { get; init; }

    public TeamPoints(Team team, int points)
    {
        Team = team;
        Points = points;
    }

    public TagCompound ToTag()
    {
        return new TagCompound
        {
            ["Team"] = (int)Team,
            ["Points"] = Points
        };
    }

    public static TeamPoints FromTag(TagCompound tag)
    {
        Team team = (Team)tag.GetInt("Team");
        int points = tag.GetInt("Points");
        return new TeamPoints(team, points);
    }
}

public readonly struct TeamBossCompletion
{
    public short BossId { get; init; }
    public Team Team { get; init; }

    public TeamBossCompletion(short bossId, Team team)
    {
        BossId = bossId;
        Team = team;
    }

    public TagCompound ToTag()
    {
        return new TagCompound
        {
            ["BossId"] = BossId,
            ["Team"] = (int)Team
        };
    }

    public static TeamBossCompletion FromTag(TagCompound tag)
    {
        short bossId = tag.GetShort("BossId");
        Team team = (Team)tag.GetInt("Team");
        return new TeamBossCompletion(bossId, team);
    }
}

