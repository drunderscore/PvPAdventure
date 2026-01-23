using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items.BiomeKeyMolds;
/// <summary>
/// An item that is just used for crafting
/// It was at one point a vanilla item, but has since been removed
/// Recipe can be found in RecipeManager
/// </summary>

public class FrozenKeyMold : ModItem
{
    public override string Texture => $"PvPAdventure/Assets/Items/FrozenKeyMold";

    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        Item.maxStack = 9999;
        Item.rare = ItemRarityID.Yellow;
    }
}
