namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    TeamBed,
    GameManager, // game manager with subpackets GameManagerPacketType
    TravelTeleport, // teleport between beds/portals/world spawn, play sound/vfx, etc
    UsePortal, // use portal creator item to create a portal, sync to everyone
    MatchStatDelta,
    Hellhex,
}
