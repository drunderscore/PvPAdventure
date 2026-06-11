using PvPAdventure.Common.Game.StatTrackers;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.GameReporters;

/// <summary>
/// Shared match-stat keys and payload helpers for PvPHub reporting.
/// </summary>
internal static class StatsReporter
{
    public const string DamageDealt = "damage_dealt";
    public const string DamageTaken = "damage_taken";
    public const string ConsumablesUsed = "consumables_used";
    public const string TilesPlaced = "tiles_placed";
    public const string TilesMined = "tiles_mined";
    public const string MiningToolsUsed = "mining_tools_used";

    private const uint MaxClientDeltaAmount = 1000;

    public static Dictionary<string, uint> CopyStats(Player player)
    {
        if (player == null)
            return [];

        return player.GetModPlayer<MatchStatsPlayer>().CopyStats();
    }

    public static Dictionary<string, IDictionary<int, uint>> CopyItemStats(Player player)
    {
        if (player == null)
            return [];

        return player.GetModPlayer<MatchStatsPlayer>().CopyItemStats();
    }

    internal static string GetStatKey(MatchStatKey statKey) => statKey switch
    {
        MatchStatKey.DamageDealt => DamageDealt,
        MatchStatKey.DamageTaken => DamageTaken,
        MatchStatKey.ConsumablesUsed => ConsumablesUsed,
        MatchStatKey.TilesPlaced => TilesPlaced,
        MatchStatKey.TilesMined => TilesMined,
        MatchStatKey.MiningToolsUsed => MiningToolsUsed,
        _ => ""
    };

    internal static bool IsValidClientDelta(MatchStatKey statKey, int itemKey, uint amount)
    {
        if (amount == 0 || amount > MaxClientDeltaAmount)
            return false;

        return statKey switch
        {
            MatchStatKey.ConsumablesUsed or MatchStatKey.TilesPlaced or MatchStatKey.MiningToolsUsed
                => IsValidItemId(itemKey),
            MatchStatKey.TilesMined
                => itemKey >= 0 && itemKey < TileLoader.TileCount,
            _ => false
        };
    }

    internal static bool IsValidItemId(int itemId) => itemId > 0 && itemId < ItemLoader.ItemCount;
}
