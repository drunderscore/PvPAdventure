using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;

internal class PointsConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 40;
        Item.height = 40;
    }
}
