using PvPAdventure.Core.Utilities;

namespace PvPAdventure.Common.MainMenu.Shop;

/// <summary>
/// Stores pre-defined TPVPA shop items.
/// </summary>
internal static class ShopItems
{
    public static readonly ShopItemDefinition AdventureCrown =
        new("adventure_crown", "Adventure Crown", "A crown for true adventurers.", 50, Ass.Icon_Dead);

    public static readonly ShopItemDefinition PinkSniperRifle =
        new("pink_sniper_rifle", "Pink Sniper Rifle", "It's pink, alright?", 10, Ass.PinkSniperRifle);

    public static readonly ShopItemDefinition RedSniperRifle =
        new("red_sniper_rifle", "Red Sniper Rifle", "It's red, alright?", 10, Ass.RedSniperRifle);

    public static readonly ShopItemDefinition[] All =
    [
        AdventureCrown,
        PinkSniperRifle,
        RedSniperRifle
    ];
}