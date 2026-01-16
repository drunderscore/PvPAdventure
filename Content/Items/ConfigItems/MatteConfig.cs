using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class MatteConfig : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 32;
        Item.height = 32;
    }
}