using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC_Old.StartItems;

internal class StartItemsPlayer : ModPlayer
{
    public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
    {
        var items = new List<Item>();
        foreach (var item in ModContent.GetInstance<AdventureServerConfig>().StartItems)
        {
            if (item.IsUnloaded)
            {
                continue;
            }

            items.Add(new Item(item.Type, stack: 1, prefix: -1));
        }

        return items;
    }
}
