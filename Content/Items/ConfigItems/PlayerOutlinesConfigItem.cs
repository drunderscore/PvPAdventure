using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class PlayerOutlinesConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 36;
        Item.height = 30;
    }
}