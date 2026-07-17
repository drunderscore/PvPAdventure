using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

public static class PlayerStatisticsNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var statistics = StatisticsPlayer.Statistics.Deserialize(reader);

        // Kills and deaths are calculated by the server. Never accept replacement
        // totals from a client, otherwise a stale SSC client can reset them.
        if (Main.netMode == NetmodeID.Server)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Warn(
                $"Ignored client statistics update from slot {whoAmI}: K={statistics.Kills}, D={statistics.Deaths}");
            return;
        }

        int playerIndex = statistics.Player;

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        var player = Main.player[playerIndex];
        if (player == null)
            return;

        statistics.Apply(player.GetModPlayer<StatisticsPlayer>());

        ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
    }
}
