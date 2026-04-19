using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;

public class CombatConfigItemBig : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 64;
        Item.height = 64;
    }
}