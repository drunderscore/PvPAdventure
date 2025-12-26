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
    SetPointsRequest,
    SSC, // Server Sided Character
    BedTeleport, // teleport to a bed spawn point
    SpawnAndSpectateCommitRespawn, // spawn selector commit respawn (random or selected teammate)
}