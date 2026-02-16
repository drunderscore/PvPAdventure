using PvPAdventure.Common.AdminTools.Tools.ArenasTool;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Common.Combat.TeamBoss;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.MatchHistory.Net;
using PvPAdventure.Common.Movement.Dash;
using PvPAdventure.Common.Security;
using PvPAdventure.Common.SpawnSelector.Net;
using PvPAdventure.Common.SSC;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Common.Visualization.HoldingMap;
using PvPAdventure.Core.Net;
using System;
using System.IO;
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

        // Helper to bind a handler with the correct parameters
        static Action Bind(BinaryReader r, int whoAmI, Action<BinaryReader, int> handler)
        {
            return () => handler(r, whoAmI);
        }

        var handler = id switch
        {
            AdventurePacketIdentifier.BountyTransaction => Bind(r, whoAmI, BountyNetHandler.HandlePacket),
            AdventurePacketIdentifier.PlayerStatistics => Bind(r, whoAmI, PlayerStatisticsNetHandler.HandlePacket),
            AdventurePacketIdentifier.PingPong => Bind(r, whoAmI, PingPongNetHandler.HandlePacket),
            AdventurePacketIdentifier.PlayerItemPickup => Bind(r, whoAmI, PlayerItemPickupNetHandler.HandlePacket),
            AdventurePacketIdentifier.PlayerTeam => Bind(r, whoAmI, PlayerTeamNetHandler.HandlePacket),
            AdventurePacketIdentifier.GameTimer => Bind(r, whoAmI, GameTimerNetHandler.HandlePacket),
            AdventurePacketIdentifier.Dash => Bind(r, whoAmI, DashInputSystem.HandlePacket),
            AdventurePacketIdentifier.TeleportRequest => Bind(r, whoAmI, TeleportNetHandler.HandlePacket),
            AdventurePacketIdentifier.PlayerBed => Bind(r, whoAmI, PlayerBedNetHandler.HandlePacket),
            AdventurePacketIdentifier.SpawnSelection => Bind(r, whoAmI, SpawnNetHandler.HandlePacket),
            AdventurePacketIdentifier.AdventureMirrorRightClickUse => Bind(r, whoAmI, AdventureMirrorNetHandler.HandlePacket),
            AdventurePacketIdentifier.TeleportFx => Bind(r, whoAmI, TeleportFxNetHandler.HandlePacket),
            AdventurePacketIdentifier.SSC => Bind(r, whoAmI, SSC.HandlePacket),
            AdventurePacketIdentifier.NpcStrikeTeam => Bind(r, whoAmI, TeamBossNetHandler.HandlePacket),
            AdventurePacketIdentifier.HoldingMap => Bind(r, whoAmI, MapHoldingNetHandler.HandlePacket),
            AdventurePacketIdentifier.SaveMatch => Bind(r, whoAmI, SaveMatchNetHandler.HandlePacket),
            AdventurePacketIdentifier.ClientModCheck => Bind(r, whoAmI, ClientModHandler.HandlePacket),
            AdventurePacketIdentifier.WhitelistPlayerCheck => Bind(r, whoAmI, WhitelistPlayerHandler.HandlePacket),
            AdventurePacketIdentifier.ArenasAdmin => Bind(r, whoAmI, ArenasAdminNetHandler.HandlePacket),
            _ => null
        };

        handler.Invoke();
    }
}
