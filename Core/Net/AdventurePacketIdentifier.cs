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
    AdventureMirrorRightClickUse, // player right click use of adventure mirror item
    HoldingMap, // player is holding the map item
    TeleportFx, // teleport effects
    SaveMatch, // save match data to disk after game ends
    ClientModCheck, // check for client mods when joining
    WhitelistPlayerCheck,
    ArenasAdmin, // admin for arenas (send players to arenas, etc)
    Skins
}