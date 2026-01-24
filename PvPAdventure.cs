using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SSC;
using System.IO;
using PvPAdventure.Common.Combat.TeamBoss;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Input.Dash;
using PvPAdventure.Core.Net;
using Terraria.ModLoader;
using PvPAdventure.Common.SpawnSelector.Net;
using PvPAdventure.Common.Bounties;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var id = (AdventurePacketIdentifier)reader.ReadByte();

        switch (id)
        {
            case AdventurePacketIdentifier.BountyTransaction:
                BountyNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerStatistics:
                PlayerStatisticsNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PingPong:
                PingPongNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerItemPickup:
                PlayerItemPickupNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerTeam:
                PlayerTeamNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.GameTimer:
                GameTimerNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.Dash:
                DashInputSystem.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TeleportRequest:
                TeleportNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.PlayerBed:
                PlayerBedNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.SpawnSelection:
                SpawnNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.AdventureMirrorRightClickUse:
                AdventureMirrorNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TeleportFx:
                TeleportFxNetHandler.Receive(reader);
                break;

            case AdventurePacketIdentifier.SSC:
                SSC.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.NpcStrikeTeam:
                TeamBossNetHandler.HandlePacket(reader, whoAmI);
                break;

        }
    }
}