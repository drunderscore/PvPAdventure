using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;
public class HPVanillaConfigItem : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 36;
        Item.height = 12;
    }
}