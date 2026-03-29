using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

public static class ProductCatalog
{
    private static readonly Dictionary<SkinIdentity, ProductDefinition> ByIdentity = new()
    {
        [new("sniper_rifle", "red")] = new(
            "sniper_rifle",
            "red",
            "Red Sniper Rifle",
            "It's red, alright?",
            50,
            Ass.SniperRifleRed,
            ItemID.SniperRifle),

        [new("sniper_rifle", "blue")] = new(
            "sniper_rifle",
            "blue",
            "Blue Sniper Rifle",
            "It's blue, alright?",
            50,
            Ass.SniperRifleBlue,
            ItemID.SniperRifle),
    };

    public static IEnumerable<ProductDefinition> All => ByIdentity.Values;

    public static bool TryGet(SkinIdentity identity, out ProductDefinition definition)
    {
        return ByIdentity.TryGetValue(identity, out definition);
    }

    public static bool TryGet(string prototype, string name, out ProductDefinition definition)
    {
        return TryGet(new SkinIdentity(prototype, name), out definition);
    }
}