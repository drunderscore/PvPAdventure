using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.Common.Items;

// - Registers circular shimmer transforms.
// - Removes prefixes (spawn/reforge/load) when configured.
// - Prevents reforging when prefixes are removed.
public class ItemPrefixesAndShimmer : GlobalItem
{
    public override void SetStaticDefaults()
    {
        void AddCircularShimmerTransform(params int[] items)
        {
            for (var i = 1; i < items.Length; i++)
                ItemID.Sets.ShimmerTransformToItem[items[i - 1]] = items[i];

            ItemID.Sets.ShimmerTransformToItem[items[^1]] = items[0];
        }

        AddCircularShimmerTransform(
            ItemID.CrystalNinjaHelmet,
            ItemID.CrystalNinjaChestplate,
            ItemID.CrystalNinjaLeggings
        );
        AddCircularShimmerTransform(
            ItemID.GladiatorHelmet,
            ItemID.GladiatorBreastplate,
            ItemID.GladiatorLeggings
        );
        AddCircularShimmerTransform(ItemID.PaladinsHammer, ItemID.PaladinsShield);
        AddCircularShimmerTransform(ItemID.MaceWhip, ItemID.Keybrand);
        AddCircularShimmerTransform(ItemID.SniperRifle, ItemID.RifleScope);
        AddCircularShimmerTransform(ItemID.Tabi, ItemID.BlackBelt);
        AddCircularShimmerTransform(ItemID.PiggyBank, ItemID.MoneyTrough);
    }

    public override bool? PrefixChance(Item item, int pre, UnifiedRandom rand)
    {
        // Prevent the item from spawning with a prefix, being placed into a reforge window, and loading with a prefix.
        if ((pre == -1 || pre == -3 || pre > 0) && ModContent.GetInstance<ServerConfig>().RemovePrefixes)
            return false;

        return null;
    }

    // This is likely unnecessary if we are overriding PrefixChance, but might as well.
    public override bool CanReforge(Item item) => !ModContent.GetInstance<ServerConfig>().RemovePrefixes;
}
