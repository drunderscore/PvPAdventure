using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class SSCConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 45;
        Item.height = 40;
    }
}