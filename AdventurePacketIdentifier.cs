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
    NpcStrikeTeam,
    Dash,
    PlayerBed,
    AdventureMirrorRightClickUse,
    BedTeleport,
    TeamSpectate
}