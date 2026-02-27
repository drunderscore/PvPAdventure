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

            case AdventurePacketIdentifier.SaveMatch:
                Common.MainMenu.MatchHistory.Net.SaveMatchNetHandler.HandlePacket(r, whoAmI);
                break;

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

            default:
                Log.Warn($"Unknown packet id: {id}");
                break;
        }
    }

    //[Obsolete("Legacy because it is less readable")]
    //public override void HandlePacket(BinaryReader r, int whoAmI)
    //{
    //    var id = (AdventurePacketIdentifier)r.ReadByte();

    //    // Helper to bind a handler with the correct parameters
    //    static Action Bind(BinaryReader r, int whoAmI, Action<BinaryReader, int> handler)
    //    {
    //        return () => handler(r, whoAmI);
    //    }

    //    var handler = id switch
    //    {
    //        AdventurePacketIdentifier.BountyTransaction => Bind(r, whoAmI, BountyNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.PlayerStatistics => Bind(r, whoAmI, PlayerStatisticsNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.PingPong => Bind(r, whoAmI, PingPongNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.PlayerItemPickup => Bind(r, whoAmI, PlayerItemPickupNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.PlayerTeam => Bind(r, whoAmI, PlayerTeamNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.GameTimer => Bind(r, whoAmI, GameTimerNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.Dash => Bind(r, whoAmI, DashInputSystem.HandlePacket),
    //        AdventurePacketIdentifier.TeleportRequest => Bind(r, whoAmI, TeleportNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.PlayerBed => Bind(r, whoAmI, PlayerBedNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.SpawnSelection => Bind(r, whoAmI, SpawnNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.AdventureMirrorRightClickUse => Bind(r, whoAmI, AdventureMirrorNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.TeleportFx => Bind(r, whoAmI, TeleportFxNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.SSC => Bind(r, whoAmI, SSC.HandlePacket),
    //        AdventurePacketIdentifier.NpcStrikeTeam => Bind(r, whoAmI, TeamBossNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.HoldingMap => Bind(r, whoAmI, MapHoldingNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.SaveMatch => Bind(r, whoAmI, SaveMatchNetHandler.HandlePacket),
    //        AdventurePacketIdentifier.ClientModCheck => Bind(r, whoAmI, ClientModHandler.HandlePacket),
    //        AdventurePacketIdentifier.WhitelistPlayerCheck => Bind(r, whoAmI, WhitelistPlayerHandler.HandlePacket),
    //        AdventurePacketIdentifier.ArenasAdmin => Bind(r, whoAmI, ArenasAdminNetHandler.HandlePacket),
    //        //AdventurePacketIdentifier.WeaponSkin => Bind(r, whoAmI, SkinNetHandler.HandlePacket),
    //        _ => null
    //    };

    //    handler.Invoke();
    //}
}
