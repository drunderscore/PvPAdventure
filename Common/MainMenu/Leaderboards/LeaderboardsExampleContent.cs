namespace PvPAdventure.Common.MainMenu.Leaderboards;

internal static class LeaderboardsExampleContent
{
    public static LeaderboardsUIContent Create()
    {
        return new LeaderboardsUIContent(
        [
            new LeaderboardEntryContent(1, "Erky", 412, 121, 58),
            new LeaderboardEntryContent(2, "BlueMage", 398, 160, 61),
            new LeaderboardEntryContent(3, "TacticalBed", 355, 144, 49),
            new LeaderboardEntryContent(4, "VolcanoMain", 301, 201, 63),
            new LeaderboardEntryContent(5, "TrainHorn", 280, 132, 40),
            new LeaderboardEntryContent(6, "VineBoom", 267, 173, 52),
            new LeaderboardEntryContent(7, "RedSniper", 240, 118, 34),
            new LeaderboardEntryContent(8, "CasualPlayer", 221, 199, 57),
            new LeaderboardEntryContent(9, "ArenaEnjoyer", 205, 142, 39),
            new LeaderboardEntryContent(10, "MirrorTech", 188, 111, 28)
        ]);
    }
}
