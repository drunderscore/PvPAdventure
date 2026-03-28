using PvPAdventure.Core.Utilities;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

public static class Products
{
    public static readonly ProductDefinition[] All =
    [
        new("influx_waver", "cyberblade", "Cyberblade", "Neon edge, clean cuts.", 200, Ass.InfluxWaverCyberblade, ItemID.InfluxWaver),
        new("light_disc", "light_disc", "Light Disc", "Wait, an actual disc?!", 200, Ass.LightDisc, ItemID.LightDisc),
        new("staff_of_earth", "avalanche_staff", "Avalanche Staff", "Meteors? More like snowballs.", 150, Ass.StaffOfEarthAvalancheStaff, ItemID.StaffofEarth),
        new("sniper_rifle", "blue", "Blue Sniper Rifle", "It's blue, alright?", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),
        new("sniper_rifle", "green", "Green Sniper Rifle", "It's green, alright?", 50, Ass.SniperRifleGreen, ItemID.SniperRifle),
        new("sniper_rifle", "pink", "Pink Sniper Rifle", "It's pink, alright?", 50, Ass.SniperRiflePink, ItemID.SniperRifle),
        new("sniper_rifle", "red", "Red Sniper Rifle", "It's red, alright?", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        new("sniper_rifle", "yellow", "Yellow Sniper Rifle", "Blessed by the Hallow.", 100, Ass.SniperRifleYellow, ItemID.SniperRifle),
        new("true_excalibur", "blossom_haze", "Blossom Haze", "Blessed by the Loku.", 200, Ass.TrueExcaliburBlossomHaze, ItemID.TrueExcalibur),
    ];
}

//new("volcano_molten","Molten Volcano","Even more volcanic.",500,Ass.VolcanoMolten,ItemID.FieryGreatsword),
//new("adventure_mirror_shimmer","Shimmer Mirror","Blessed by the Aether.",500,Ass.AdventureMirrorShimmer,ModContent.ItemType<AdventureMirror>()),