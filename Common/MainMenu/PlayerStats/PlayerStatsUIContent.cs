namespace PvPAdventure.Common.MainMenu.PlayerStats;

internal readonly record struct PlayerStatsUIContent(
    int Kills,
    int Deaths,
    int Wins,
    int Losses,
    int TeamPointsTotal);
