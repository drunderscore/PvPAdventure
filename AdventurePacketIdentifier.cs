namespace PvPAdventure;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    PingPong,
    PlayerItemPickup,
    PlayerTeam,
    NpcStrikeTeam,
    Dash,
    PlayerBed,
    AdventureMirrorRightClickUse,
    BedTeleport,
    QueueToggle,        
    QueueCounts,        
    QueueCountsRequest
}