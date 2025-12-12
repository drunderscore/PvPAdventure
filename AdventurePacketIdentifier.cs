namespace PvPAdventure;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    PingPong,
    PlayerItemPickup,
    PlayerTeam,
    StartGame,
    EndGame,
    PauseGame,
    NpcStrikeTeam,
    Dash,
    PlayerBed,
    AdventureMirrorRightClickUse,
    BedTeleport,
}