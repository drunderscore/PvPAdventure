using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>
/// Keeps track of mining tools used and tile IDs mined during a match.
/// </summary>
internal sealed class MineTracker : GlobalTile
{
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (fail || effectOnly || Main.dedServ)
            return;

        if (!AchievementsHelper.CurrentlyMining)
            return;

        Player player = Main.LocalPlayer;
        if (player == null || !player.active || player.HeldItem == null || player.HeldItem.IsAir)
            return;

        Item heldItem = player.HeldItem;
        if (heldItem.pick <= 0 && heldItem.axe <= 0 && heldItem.hammer <= 0)
            return;

        MatchStatsPlayer.RecordLocalItemStat(MatchStatKey.TilesMined, type);
        MatchStatsPlayer.RecordLocalItemStat(MatchStatKey.MiningToolsUsed, heldItem.type);
    }
}
