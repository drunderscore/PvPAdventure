using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class ArenasConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 80;
        Item.height = 80;
    }
}