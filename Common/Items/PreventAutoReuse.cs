using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

internal class PreventAutoReuse : GlobalItem
{
    public override bool? CanAutoReuseItem(Item item, Player player)
    {
        if (ModContent.GetInstance<ServerConfig>().PreventAutoReuse.Contains(new(item.type)))
            return false;

        return null;
    }
}
