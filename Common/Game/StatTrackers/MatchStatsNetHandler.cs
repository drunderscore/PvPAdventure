using PvPAdventure.Common.Game.GameReporters;
using System.IO;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.Game.StatTrackers;

internal static class MatchStatsNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        MatchStatsPlayer.StatDelta delta = MatchStatsPlayer.StatDelta.Deserialize(reader);

        if (Main.netMode != NetmodeID.Server)
            return;

        if (!StatsReporter.IsValidClientDelta(delta.StatKey, delta.ItemKey, delta.Amount))
            return;

        GameManager gameManager = Terraria.ModLoader.ModContent.GetInstance<GameManager>();
        if (gameManager.CurrentPhase != GameManager.Phase.Playing)
            return;

        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
            return;

        Player player = Main.player[whoAmI];
        if (player == null || !player.active)
            return;

        player.GetModPlayer<MatchStatsPlayer>().ApplyDelta(delta.StatKey, delta.ItemKey, delta.Amount);
    }
}
