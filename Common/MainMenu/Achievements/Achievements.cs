using System.Linq;

namespace PvPAdventure.Common.MainMenu.Achievements;

/// <summary>
/// Store all pre-defined TPVPA achievements.
/// </summary>
public static class Achievements
{
    public static readonly AchievementDefinition MatchMade = new(
        IconIndex: 82,
        Title: "Match Made",
        Description: "Finish your first TPVPA match.",
        Target: 1,
        GemsReward: 10,
        Delta: _ => 1);

    public static readonly AchievementDefinition FirstBlood = new(
        IconIndex: 82,
        Title: "First Blood",
        Description: "Get your first kill in TPVPA.",
        Target: 1,
        GemsReward: 20,
        Delta: match =>
        {
            var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
            if (p == null) return 0;
            return p.Value.Kills > 0 ? 1 : 0;
        });

    public static readonly AchievementDefinition TeamPlayer = new(
        IconIndex: 82,
        Title: "Team Player",
        Description: "Earn 200 team points in a single match.",
        Target: 1,
        GemsReward: 30,
        Delta: match =>
        {
            var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
            if (p == null) return 0;

            var tp = match.TeamPoints?.FirstOrDefault(x => x.Team == p.Value.Team);
            if (tp == null) return 0;

            return tp.Value.Points >= 200 ? 1 : 0;
        });

    public static readonly AchievementDefinition ThirtyPieces = new(
        IconIndex: 82,
        Title: "Thirty Pieces",
        Description: "Get 30 kills in a single TPVPA match.",
        Target: 1,
        GemsReward: 45,
        Delta: match =>
        {
            var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
            if (p == null) return 0;
            return p.Value.Kills >= 30 ? 1 : 0;
        });

    public static readonly AchievementDefinition HalfCentury = new(
        IconIndex: 82,
        Title: "Half Century",
        Description: "Get 50 kills in a single TPVPA match.",
        Target: 1,
        GemsReward: 65,
        Delta: match =>
        {
            var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
            if (p == null) return 0;
            return p.Value.Kills >= 50 ? 1 : 0;
        });

    public static readonly AchievementDefinition OnARoll = new(
        IconIndex: 82,
        Title: "On a Roll",
        Description: "Win 5 TPVPA matches.",
        Target: 5,
        GemsReward: 85,
        Delta: match => match.Win ? 1 : 0);

    public static readonly AchievementDefinition BigWinner = new(
        IconIndex: 82,
        Title: "Big Winner",
        Description: "Win 25 TPVPA matches.",
        Target: 25,
        GemsReward: 130,
        Delta: match => match.Win ? 1 : 0);

    public static readonly AchievementDefinition Veteran = new(
        IconIndex: 82,
        Title: "Veteran",
        Description: "Finish 50 TPVPA matches.",
        Target: 50,
        GemsReward: 150,
        Delta: _ => 1);

    public static readonly AchievementDefinition HundredClub = new(
        IconIndex: 82,
        Title: "Hundred Club",
        Description: "Get 100 kills in a single TPVPA match.",
        Target: 1,
        GemsReward: 500,
        Delta: match =>
        {
            var p = match.Players?.FirstOrDefault(x => (ulong)x.SteamId == (ulong)match.LocalSteamId);
            if (p == null) return 0;
            return p.Value.Kills >= 100 ? 1 : 0;
        });

    public static readonly AchievementDefinition TopDog = new(
        IconIndex: 82,
        Title: "Top Dog",
        Description: "Win 50 TPVPA matches.",
        Target: 50,
        GemsReward: 500,
        Delta: match => match.Win ? 1 : 0);

    /// <summary>
    /// This field provides a comprehensive list of all available achievements.
    /// New achievements must be added here and follow the same pattern as the existing ones to be properly displayed and tracked in the UI.
    /// Used to display achievements in <see cref="UI.AchievementsUIState"/>
    /// </summary>
    public static readonly (string Id, AchievementDefinition Def)[] All =
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