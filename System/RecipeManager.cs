using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.System;

public class RecipeManager : ModSystem
{
    public override void AddRecipes()
    {
        for (int i = 0; i < ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes.Count; i++)
        {
            for (int result = 0; result < ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].EnemyDrops.Count; result++)
            {
                for (int material = 0; material < ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].EnemyDrops.Count; material++)
                {
                    Recipe recipe;
                    if (result == material)
                        continue;

                    recipe = Recipe.Create(ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].EnemyDrops[result].Type)
                        .AddIngredient(ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].EnemyDrops[material].Type, ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].AmountNeeded);

                    if (ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].Workstation.Type != -1)
                        recipe.AddTile(ModContent.GetInstance<AdventureConfig>().DuplicateItemRecipes[i].Workstation.Type);

                    recipe.Register();
                }
            }
        }
    }
}