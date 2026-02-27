using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Shop;
public sealed class RedSniperRifle : ModItem
{
    public override string Texture => "PvPAdventure/Assets/Shop/RedSniperRifle";

    public override void SetStaticDefaults()
    {
        // Set custom display name via localization key:
        // Mods.PvPAdventure.ItemName.PinkSniperRifle = "Pink Sniper Rifle"
    }

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.SniperRifle);
    }
}