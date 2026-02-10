using System;
using System.Linq;

namespace PvPAdventure.Common.MatchHistory.Achievements;

public static class Achievements
{
    public static readonly Achievement MatchMade = new(2, "Match Made", "Finish your first TPVPA match.", 1, _ => 1);

    public static readonly Achievement FirstBlood = new(5, "First Blood", "Get your first kill in TPVPA.", 1, match =>
    {
        var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
        if (p == null) return 0;
        return p.Value.Kills > 0 ? 1 : 0;
    });

    public static readonly Achievement TeamPlayer = new(9, "Team Player", "Earn 200 team points in a single match.", 1, match =>
    {
        var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
        if (p == null) return 0;

        var tp = match.TeamPoints?.FirstOrDefault(x => x.Team == p.Value.Team);
        if (tp == null) return 0;

        return tp.Value.Points >= 200 ? 1 : 0;
    });

    public static readonly Achievement ThirtyPieces = new(6, "Thirty Pieces", "Get 30 kills in a single TPVPA match.", 1, match =>
    {
        var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
        if (p == null) return 0;
        return p.Value.Kills >= 30 ? 1 : 0;
    });

    public static readonly Achievement HalfCentury = new(7, "Half Century", "Get 50 kills in a single TPVPA match.", 1, match =>
    {
        var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
        if (p == null) return 0;
        return p.Value.Kills >= 50 ? 1 : 0;
    });

    public static readonly Achievement OnARoll = new(3, "On a Roll", "Win 5 TPVPA matches.", 5, match => match.Win ? 1 : 0);

    public static readonly Achievement BigWinner = new(4, "Big Winner", "Win 25 TPVPA matches.", 25, match => match.Win ? 1 : 0);

    public static readonly Achievement Veteran = new(11, "Veteran", "Finish 50 TPVPA matches.", 50, _ => 1);

    public static readonly Achievement HundredClub = new(8, "Hundred Club", "Get 100 kills in a single TPVPA match.", 1, match =>
    {
        var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
        if (p == null) return 0;
        return p.Value.Kills >= 100 ? 1 : 0;
    });

    public static readonly Achievement TopDog = new(12, "Top Dog", "Win 50 TPVPA matches.", 50, match => match.Win ? 1 : 0);

    public static readonly (string Id, Achievement Def)[] All =
    [
        // easiest -> hardest
        (nameof(MatchMade), MatchMade),
        (nameof(FirstBlood), FirstBlood),
        (nameof(TeamPlayer), TeamPlayer),
        (nameof(ThirtyPieces), ThirtyPieces),
        (nameof(HalfCentury), HalfCentury),
        (nameof(OnARoll), OnARoll),
        (nameof(BigWinner), BigWinner),
        (nameof(Veteran), Veteran),

        // hardest
        (nameof(HundredClub), HundredClub),
        (nameof(TopDog), TopDog),
    ];
}

public sealed record Achievement(int IconIndex, string Title, string Description, int Target, Func<MatchResult, int> Delta);
