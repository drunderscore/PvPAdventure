using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.ConfigItems;

public class CombatConfigItemSmall : ModItem
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        Item.width = 79;
        Item.height = 60;
    }
}