using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>
/// Keeps track of consumables/potions/food used during a match for each player.
/// </summary>
internal sealed class ConsumableUseTracker : GlobalItem
{
    public override void OnConsumeItem(Item item, Player player)
    {
        if (item == null || item.IsAir)
            return;

        MatchStatsPlayer.RecordLocalItemStat(MatchStatKey.ConsumablesUsed, item.type);
    }
}
