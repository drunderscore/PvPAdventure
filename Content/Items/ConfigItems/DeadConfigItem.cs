using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class DeadConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 22;
        Item.height = 24;
    }
}