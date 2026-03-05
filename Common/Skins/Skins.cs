// ============================================================
//  SKIN CATALOG — add new skins here
//
//  One line per skin:
//  new("unique_id", "Display Name", "Tooltip description.", price, Ass.YourTextureHere, ItemID.TheItem)
//
//  Parameters:
//    - unique_id   : lowercase, underscores only, must be unique
//    - price       : gem cost
//    - Ass.XYZ     : texture asset from AssetLoader.cs
//    - ItemID.XYZ  : vanilla item this skin applies to (e.g. ItemID.SniperRifle)
// ============================================================

using PvPAdventure.Content.Items;
using PvPAdventure.Core.Utilities;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

public static class Skins
{
    public static readonly SkinDefinition[] All =
    [
        new("influx_waver_cyberblade","Cyberblade","Neon edge, clean cuts.",200,Ass.InfluxWaverCyberblade,ItemID.InfluxWaver),
        new("light_disc","Light Disc","Wait, an actual disc?!",200,Ass.LightDisc,ItemID.LightDisc),
        new("staff_of_earth_avalanche_staff","Avalanche Staff","Meteors? More like snowballs.",150,Ass.StaffOfEarthAvalancheStaff,ItemID.StaffofEarth),
        new("sniper_rifle_blue","Blue Sniper Rifle","It's blue, alright?",50,Ass.SniperRifleBlue,ItemID.SniperRifle),
        new("sniper_rifle_green","Green Sniper Rifle","It's green, alright?",50,Ass.SniperRifleGreen,ItemID.SniperRifle),
        new("sniper_rifle_pink","Pink Sniper Rifle","It's pink, alright?",50,Ass.SniperRiflePink,ItemID.SniperRifle),
        new("sniper_rifle_red","Red Sniper Rifle","It's red, alright?",50,Ass.SniperRifleRed,ItemID.SniperRifle),
        new("sniper_rifle_yellow","Yellow Sniper Rifle","Blessed by the Hallow.",100,Ass.SniperRifleYellow,ItemID.SniperRifle),
        //new("volcano_molten","Molten Volcano","Even more volcanic.",500,Ass.VolcanoMolten,ItemID.FieryGreatsword),
        //new("adventure_mirror_shimmer","Shimmer Mirror","Blessed by the Aether.",500,Ass.AdventureMirrorShimmer,ModContent.ItemType<AdventureMirror>()),
        new("true_excalibur_blossom_haze","Blossom Haze","Blessed by the Loku.",200,Ass.TrueExcaliburBlossomHaze,ItemID.TrueExcalibur),
    ];
}