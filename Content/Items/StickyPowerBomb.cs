using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;

/// <summary>
/// The Sticky Power Bomb item from Terraria 1.4.5, implemented in TPVPA to serve our purposes.
/// </summary>
internal class StickyPowerBomb : ModItem
{
    public override string Texture => "PvPAdventure/Assets/Items/StickyPowerBomb";

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 22;

        Item.damage = 0;
        Item.knockBack = 0f;
        Item.useTime = 40;
        Item.useAnimation = 40;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.shoot = ModContent.ProjectileType<Projectiles.StickyPowerBombProjectile>();
        Item.shootSpeed = 6f;

        Item.consumable = true;
        Item.maxStack = 9999;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = false;

        Item.DamageType = DamageClass.Summon;
        Item.value = Item.sellPrice(silver: 2);
        Item.rare = ItemRarityID.LightRed;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<PowerBomb>())
            .AddIngredient(ItemID.Gel, 1)
            .Register();
    }

    public override bool? UseItem(Player player) => true;

    public override bool ConsumeItem(Player player) => true;
}