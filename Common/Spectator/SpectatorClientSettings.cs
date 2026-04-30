namespace PvPAdventure.Common.Spectator;

internal enum SpectatorPlayerDrawMode
{
    FullPlayer,
    PlayerHeads,
    None
}

internal static class SpectatorClientSettings
{
    public static SpectatorPlayerDrawMode DrawPlayers { get; set; } = SpectatorPlayerDrawMode.FullPlayer;

    public static void CycleDrawPlayers()
    {
        DrawPlayers = DrawPlayers switch
        {
            SpectatorPlayerDrawMode.FullPlayer => SpectatorPlayerDrawMode.PlayerHeads,
            SpectatorPlayerDrawMode.PlayerHeads => SpectatorPlayerDrawMode.None,
            _ => SpectatorPlayerDrawMode.FullPlayer
        };
    }

    public static string DrawPlayersLabel => DrawPlayers switch
    {
        SpectatorPlayerDrawMode.FullPlayer => "Full Player",
        SpectatorPlayerDrawMode.PlayerHeads => "Player Heads",
        _ => "None"
    };
}
