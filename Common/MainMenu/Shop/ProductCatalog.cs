using PvPAdventure.Common.Skins;
using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

public static class ProductCatalog
{
    private static readonly Dictionary<SkinIdentity, ProductDefinition> ByIdentity = new()
    {
        [new(Prototype: "sniper_rifle", Name: "red")] = new(
            Prototype: "sniper_rifle",
            Name: "red",
            DisplayName: "Red Sniper Rifle",
            Description: "It's red, alright?",
            Price: 50,
            Texture: Ass.SniperRifleRed,
            ItemType: ItemID.SniperRifle),

        [new(Prototype: "sniper_rifle", Name: "blue")] = new(
            Prototype: "sniper_rifle",
            Name: "blue",
            DisplayName: "Blue Sniper Rifle",
            Description: "It's blue, alright?",
            Price: 50,
            Texture: Ass.SniperRifleBlue,
            ItemType: ItemID.SniperRifle),
    };

    public static IEnumerable<ProductDefinition> All => ByIdentity.Values;

    public static bool TryGet(SkinIdentity identity, out ProductDefinition definition)
    {
        return ByIdentity.TryGetValue(identity, out definition);
    }

    public static bool TryGet(string prototype, string name, out ProductDefinition definition)
    {
        return TryGet(new SkinIdentity(Prototype: prototype, Name: name), out definition);
    }
}
