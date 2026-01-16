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
    StartGame, // game manager
    AdjustGameTime, // game manager
    EndGame, // game manager
    PauseGame, // game manager
    SetPoints, // game manager
    SSC, // Server Sided Character
    PlayerBed, // update player spawn point
    TeleportRequest, // teleport to beds or world spawn
    SpawnSelection, // set random or player spawn while respawn timer or map timer is running
    AdventureMirrorRightClickUse,
    HoldingMap,
    TeleportFx, // teleport effects
    ArenaPlayerCount
}