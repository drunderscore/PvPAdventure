namespace PvPAdventure.Core.Net;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    PingPong,
    PlayerItemPickup,
    PlayerTeam,
    Dash
}