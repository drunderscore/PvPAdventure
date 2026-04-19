namespace PvPAdventure.Common.MainMenu.Leaderboards;

internal readonly record struct LeaderboardsUIContent(LeaderboardEntryContent[] Entries);

internal readonly record struct LeaderboardEntryContent(
    int Rank,
    string Player,
    int Kills,
    int Deaths,
    int Games);
