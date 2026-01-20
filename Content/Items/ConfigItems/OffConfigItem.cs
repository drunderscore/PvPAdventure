using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class OffConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 22;
        Item.height = 24;
    }
}