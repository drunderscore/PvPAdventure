namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    PlayerItemPickup,
    PlayerTeam,
    TeamBed,
    NpcStrikeTeam,
    Dash, // player keybind dash ability
    GameTimer, // game manager with subpackets GameTimerPacketType
    SSC, // Server Sided Character with subpackets SSCPacketType
    HoldingMap, // player is holding the map item
    ClientModCheck, // check for client mods when joining
    ArenasAdmin, // admin for arenas (send players to arenas, etc)
    Skins, // sync who has skins for all players
    Spectator, // sync spectator mode for all players
    SessionTracker, // sync session time for all players
    TravelTeleport, // teleport between beds/portals/world spawn, play sound/vfx, etc
    UsePortal, // use portal creator item to create a portal, sync to everyone
}