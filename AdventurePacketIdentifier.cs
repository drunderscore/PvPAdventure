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
    PlayerBed, // update player spawn point
    AdventureMirrorRightClickUse,
    BedTeleport, // teleport to a bed spawn point
    TeamSpectate, // sync world region for spectating player
    SetPointsRequest
}