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
    TravelTeleport, // teleport between beds/portals/world spawn, play sound/vfx, etc
    UsePortal, // use portal creator item to create a portal, sync to everyone
    TeamItem, // team-owned furniture/banner item placed in the world
}
