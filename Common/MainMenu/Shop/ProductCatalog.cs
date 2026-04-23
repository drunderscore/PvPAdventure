using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

public static class ProductCatalog
{
    private static readonly Dictionary<ProductKey, ShopProduct> ByKey = new()
    {
        [new("sniper_rifle", "red")] = new(
            Prototype: "sniper_rifle",
            Name: "red",
            Texture: Ass.SniperRifleRed,
            ItemType: ItemID.SniperRifle),

        [new("sniper_rifle", "blue")] = new(
            Prototype: "sniper_rifle",
            Name: "blue",
            Texture: Ass.SniperRifleBlue,
            ItemType: ItemID.SniperRifle),
    };

    public static IEnumerable<ShopProduct> All => ByKey.Values;

    public static bool TryGet(ProductKey key, out ShopProduct definition)
    {
        return ByKey.TryGetValue(key, out definition);
    }

    public static bool TryGet(string prototype, string name, out ShopProduct definition)
    {
        return TryGet(new ProductKey(prototype, name), out definition);
    }
}