using PvPAdventure.Core.Net;
using System.IO;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    /// <summary>
    /// Packet handler for PvP Adventure mod packets.
    /// See <see cref="AdventurePacketIdentifier"/> for packet types.
    /// </summary>
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        // This causes read underflow and bogus logs, dont do it!
        //long packetStart = reader.BaseStream.Position;
        //long packetLength = reader.BaseStream.Length;

        var id = (AdventurePacketIdentifier)reader.ReadByte();

        //Log.Debug($"[Packet] Start id={(byte)id} ({id}), whoAmI={whoAmI}, bytes={packetLength - packetStart}");

        switch (id)
        {
            case AdventurePacketIdentifier.BountyTransaction:
                Common.Bounties.BountyNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerStatistics:
                Common.Statistics.PlayerStatisticsNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerItemPickup:
                Common.Statistics.PlayerItemPickupNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerTeam:
                Common.Teams.PlayerTeamNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TeamBed:
                Common.Travel.Beds.TeamBedNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.NpcStrikeTeam:
                Common.Combat.TeamBoss.TeamBossNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.Dash:
                Common.Movement.Dash.DashInputSystem.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.GameTimer:
                Common.GameTimer.GameTimerNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.SSC:
                Common.SSC.SSC.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.ClientModCheck:
                Common.Security.ClientModHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.HoldingMap:
                Common.Visualization.HoldingMap.MapHoldingNetHandler.HandlePacket(reader, whoAmI);
                break;

            //case AdventurePacketIdentifier.ArenasAdmin:
            //    Common.AdminTools.Tools.ArenasTool.ArenasAdminNetHandler.HandlePacket(reader, whoAmI);
            //    break;

            case AdventurePacketIdentifier.Skins:
                Common.Skins.SkinNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.Spectator:
                Common.Spectator.Net.SpectatorModeNetHandler.Receive(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.SessionTracker:
                Common.Spectator.Trackers.SessionTrackerNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TravelTeleport:
                Common.Travel.TravelTeleportNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.UsePortal:
                Common.Travel.Portals.PortalNetHandler.HandlePacket(reader, whoAmI);
                break;

            default:
                Log.Warn($"[Packet] Unknown packet id: {(byte)id} ({id})");
                break;
        }

        //long bytesLeft = reader.BaseStream.Length - reader.BaseStream.Position;

        //if (bytesLeft != 0)
        //{
        //    Log.Warn($"[Packet] Handler left unread bytes: id={(byte)id} ({id}), left={bytesLeft}, total={packetLength - packetStart}");
        //    reader.BaseStream.Position = reader.BaseStream.Length;
        //}
    }
}
