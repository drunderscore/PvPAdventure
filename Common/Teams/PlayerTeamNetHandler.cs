using PvPAdventure.Common.AdminTools.Tools.TeamAssigner;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Teams;

public static class PlayerTeamNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var team = Team.Deserialize(reader);

        if (Main.netMode == NetmodeID.Server)
        {
            if (team.Player < 0 || team.Player >= Main.maxPlayers)
            {
                return;
            }

            Player target = Main.player[team.Player];
            if (target == null || !target.active)
            {
                return;
            }

            target.team = (int)team.Value;

            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            team.Serialize(packet);
            packet.Send();

            return;
        }

        if (team.Player >= 0 && team.Player < Main.maxPlayers)
        {
            Player target = Main.player[team.Player];
            if (target != null && target.active)
            {
                target.team = (int)team.Value;
            }
        }

        if (!Main.dedServ)
        {
            ModContent.GetInstance<PointsManager>().UiScoreboard?.Invalidate();
        }

        var ts = ModContent.GetInstance<TeamAssignerSystem>();
        if (ts?.teamAssignerState != null)
        {
            foreach (var child in ts.teamAssignerState.Children)
            {
                if (child is TeamAssignerPanel panel)
                {
                    panel.needsRebuild = true;
                    break;
                }
            }
        }
    }
}
