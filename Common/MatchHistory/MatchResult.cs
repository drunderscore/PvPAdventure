using System;
using System.Globalization;
using System.Text;
using Terraria.Enums;

namespace PvPAdventure.Common.MatchHistory;

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
}