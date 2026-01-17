using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

public static class PlayerStatisticsNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var statistics = StatisticsPlayer.Statistics.Deserialize(reader);

        int playerIndex;
        if (Main.dedServ)
        {
            playerIndex = whoAmI;
        }
        else
        {
            playerIndex = statistics.Player;
        }

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
        {
            return;
        }

        var player = Main.player[playerIndex];
        if (player == null)
        {
            return;
        }

        statistics.Apply(player.GetModPlayer<StatisticsPlayer>());

        if (!Main.dedServ)
        {
            ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
        }
    }
}
