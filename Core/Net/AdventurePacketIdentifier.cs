namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    PlayerTeam, // send team info to server when joining, or to clients when team changes
    BountyTransaction,
    TeamBed,
    GameManager, // game manager with subpackets GameManagerPacketType
    TravelTeleport, // teleport between beds/portals/world spawn, play sound/vfx, etc
    UsePortal, // use portal creator item to create a portal, sync to everyone
    MatchStatDelta,
}
