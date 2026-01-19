using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Common.SpawnSelector.UI;
using PvPAdventure.Common.SSC;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.Combat.TeamBoss;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Input.Dash;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    public override void Load()
    {
        // This mod should only ever be loaded when connecting to a server, it should never be loaded beforehand.
        // We don't use Netplay.Disconnect here, as that's not initialized to true (but rather to default value, aka false), so instead
        // we'll check the connection status of our own socket.
        //if (Main.dedServ)
        //{
        //    ModContent.GetInstance<DiscordIdentification>().PlayerJoin += (_, args) =>
        //    {
        //        // FIXME: We should allow or deny players based on proper criteria.
        //        //        For now, let's allow everyone.
        //        args.Allowed = true;
        //    };
        //}
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var id = (AdventurePacketIdentifier)reader.ReadByte();

        switch (id)
        {
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
            }
            case AdventurePacketIdentifier.NpcStrikeTeam:
                TeamBossNetHandler.HandlePacket(reader, whoAmI);
                break;
        }
    }
}