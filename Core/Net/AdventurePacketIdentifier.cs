namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    PingPong,
    PlayerItemPickup,
    PlayerTeam,
    NpcStrikeTeam,
    Dash, // player keybind dash ability
    GameTimer, // game manager with subpackets GameTimerPacketType
    SSC, // Server Sided Character with subpackets SSCPacketType
    PlayerBed, // update player spawn point
    TeleportRequest, // teleport to beds or world spawn
    SpawnSelection, // set random or player spawn while respawn timer or map timer is running
    AdventureMirrorRightClickUse,
    HoldingMap,
    TeleportFx // teleport effects
}