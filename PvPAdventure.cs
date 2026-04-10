using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    /// <summary>
    /// Packet handler for PvP Adventure mod packets.
    /// See <see cref="AdventurePacketIdentifier"/> for packet types.
    /// </summary>
    public override void HandlePacket(BinaryReader r, int whoAmI)
    {
        var id = (AdventurePacketIdentifier)r.ReadByte();

        switch (id)
        {
            case AdventurePacketIdentifier.BountyTransaction:
                Common.Bounties.BountyNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerStatistics:
                Common.Statistics.PlayerStatisticsNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.PingPong:
                PingPongNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerItemPickup:
                Common.Statistics.PlayerItemPickupNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerTeam:
                Common.Teams.PlayerTeamNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.GameTimer:
                Common.GameTimer.GameTimerNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.Dash:
                Common.Movement.Dash.DashInputSystem.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.TeleportRequest:
                Common.SpawnSelector.Net.TeleportNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerBed:
                Common.SpawnSelector.Net.PlayerBedNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.SpawnSelection:
                Common.SpawnSelector.Net.SpawnNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.AdventureMirrorRightClickUse:
                Common.SpawnSelector.Net.AdventureMirrorNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.TeleportFx:
                Common.SpawnSelector.Net.TeleportFxNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.SSC:
                Common.SSC.SSC.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.NpcStrikeTeam:
                Common.Combat.TeamBoss.TeamBossNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.HoldingMap:
                Common.Visualization.HoldingMap.MapHoldingNetHandler.HandlePacket(r, whoAmI);
                break;

            //case AdventurePacketIdentifier.SaveMatch:
            //    Common.MainMenu.MatchHistory.Net.SaveMatchNetHandler.HandlePacket(r, whoAmI);
            //    break;

            case AdventurePacketIdentifier.ClientModCheck:
                Common.Security.ClientModHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.WhitelistPlayerCheck:
                Common.Security.WhitelistPlayerHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.ArenasAdmin:
                Common.AdminTools.Tools.ArenasTool.ArenasAdminNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.Skins:
                Common.Skins.SkinNetHandler.HandlePacket(r, whoAmI);
                break;

            case AdventurePacketIdentifier.Spectator:
                Common.Spectator.SpectatorNetHandler.Receive(r, whoAmI);
                break;

            case AdventurePacketIdentifier.SessionTracker:
                Common.Spectator.Trackers.SessionTrackerNetHandler.HandlePacket(r, whoAmI);
                break;

            default:
                Log.Warn($"Unknown packet id: {id}");
                break;
        }
    }
}
