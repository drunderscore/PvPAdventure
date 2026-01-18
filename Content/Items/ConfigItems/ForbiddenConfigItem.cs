using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class ForbiddenConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 28;
        Item.height = 28;
    }
}