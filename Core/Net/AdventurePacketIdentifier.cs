namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    // 0 was the removed custom PlayerTeam packet. Keep it reserved so existing packet IDs do not shift.
    BountyTransaction = 1,
    TeamBed = 2,
    GameManager = 3, // game manager with subpackets GameManagerPacketType
    TravelTeleport = 4, // teleport between beds/portals/world spawn, play sound/vfx, etc
    UsePortal = 5, // use portal creator item to create a portal, sync to everyone
    MatchStatDelta = 6,
}
