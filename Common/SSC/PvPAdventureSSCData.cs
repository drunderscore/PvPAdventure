using PvPAdventure.Common.Statistics;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Extra functionality for SSC, specific to PvPAdventure.
/// This is not part of the base SSC mod, but is used to ensure that PvPAdventure's statistics system and more works properly with SSC.
/// </summary>
public static class PvPAdventureSSCData
{
    public static void ApplyStatsToServerPlayer(int whoAmI, TagCompound root)
    {
        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
            return;

        Player p = Main.player[whoAmI];
        if (p == null || !p.active)
            return;

        if (!root.ContainsKey("PvPAdventureSSC"))
            return;

        TagCompound ssc = root.GetCompound("PvPAdventureSSC");

        var stats = p.GetModPlayer<StatisticsPlayer>();
        stats.ApplySscOverride(ssc);

        // Push the corrected values to everyone (including the joining client)
        // so the server does not overwrite SSC-loaded client state with 0/0.
        typeof(StatisticsPlayer)
            .GetMethod("SyncStatistics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(stats, [-1, -1]);
    }

    public static void SavePvPAdventureStats(Player player, TagCompound root)
    {
        var stats = player.GetModPlayer<StatisticsPlayer>();

        var sscTag = new TagCompound
        {
            ["kills"] = stats.Kills,
            ["deaths"] = stats.Deaths,
            ["itemPickups"] = stats.ItemPickups.ToArray(),
            ["team"] = player.team
        };

        PlayerPositionSystem.SavePlayerPosition(player, sscTag);

        root["PvPAdventureSSC"] = sscTag;
    }
}
