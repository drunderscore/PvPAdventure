using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

internal class NoShimmerAlchemy : ModSystem
{
    public override void PostAddRecipes()
    {
        for (int i = 0; i < Recipe.numRecipes; i++)
        {
            Recipe recipe = Main.recipe[i];

            // If this recipe requires a Placed Bottle / Alchemy Table, disable decrafting
            if (recipe.requiredTile.Contains(TileID.Bottles))
            {
                recipe.DisableDecraft();
            }
        }
    }
}