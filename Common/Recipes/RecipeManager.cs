using PvPAdventure.Content.Items.BiomeKeyMolds;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Recipes;

[Autoload(Side = ModSide.Both)]
// - This is where we add new recipes, remove recipes, and change recipes
// - Right now, in order to change a recipe, we just remove the vanilla recipe, and add a new modded recipe, which might be inefficient 
public class RecipeManager : ModSystem
{

    public override void AddRecipes()
    {
        

        // List of items to disable the recipes of, usually in ortder to replace it with our own new recipe

        int[] itemsToRemove = new int[]
        {
            ItemID.MoonlordArrow,
            ItemID.ShroomiteBar,
            ItemID.WormFood,
            ItemID.ChlorophyteBar,
            ItemID.FlaskofCursedFlames,
            ItemID.FlaskofIchor,
            ItemID.FlaskofPoison,
            ItemID.FlaskofVenom,
            ItemID.FlaskofFire,
            ItemID.FlaskofNanites,
            ItemID.FlaskofParty,
            ItemID.SpectreBar,
            ItemID.SuspiciousLookingEye,
            ItemID.ManaCrystal


        };

        for (int i = 0; i < Main.recipe.Length; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.createItem.type != ItemID.None && itemsToRemove.Contains(recipe.createItem.type))
            {
                recipe.DisableRecipe();
            }
        }

        // Headstone Recipe
        Recipe.Create(ItemID.Headstone)
            .AddIngredient(ItemID.StoneBlock, 50)
            .AddTile(TileID.HeavyWorkBench)
            .Register();

        Recipe.Create(ItemID.ManaCrystal)
            .AddIngredient(ItemID.FallenStar, 3)
            .Register();


        // Corruption Key Recipe
        Recipe.Create(ItemID.CorruptionKey)
            .AddIngredient(ItemID.TempleKey, 1)
            .AddIngredient(ModContent.ItemType<CorruptionKeyMold>())
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddIngredient(ItemID.SoulofSight, 5)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddTile(TileID.MythrilAnvil)
            .Register();

        // Frozen Key Recipe
        Recipe.Create(ItemID.FrozenKey)
            .AddIngredient(ItemID.TempleKey, 1)
            .AddIngredient(ModContent.ItemType<FrozenKeyMold>())
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddIngredient(ItemID.SoulofSight, 5)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddTile(TileID.MythrilAnvil)
            .Register();

        // Jungle Key Recipe
        Recipe.Create(ItemID.JungleKey)
            .AddIngredient(ItemID.TempleKey, 1)
            .AddIngredient(ModContent.ItemType<JungleKeyMold>())
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddIngredient(ItemID.SoulofSight, 5)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddTile(TileID.MythrilAnvil)
            .Register();

        // Hallowed Key Recipe
        Recipe.Create(ItemID.HallowedKey)
            .AddIngredient(ItemID.TempleKey, 1)
            .AddIngredient(ModContent.ItemType<HallowedKeyMold>())
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddIngredient(ItemID.SoulofSight, 5)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddTile(TileID.MythrilAnvil)
            .Register();

        // Desert Key Recipe
        Recipe.Create(ItemID.DungeonDesertKey)
            .AddIngredient(ItemID.TempleKey, 1)
            .AddIngredient(ModContent.ItemType<DesertKeyMold>())
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddIngredient(ItemID.SoulofSight, 5)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddTile(TileID.MythrilAnvil)
            .Register();
        // Make Shroomite Bars cheaper
        Recipe.Create(ItemID.ShroomiteBar)
            .AddIngredient(ItemID.GlowingMushroom, 3)
            .AddIngredient(ItemID.ChlorophyteBar, 1)
            .AddTile(TileID.Autohammer)
            .Register();
        // Make suspicious eyes cheaper
        Recipe.Create(ItemID.SuspiciousLookingEye)
            .AddIngredient(ItemID.Lens, 3)
            .AddTile(TileID.DemonAltar)
            .Register();
        // Make worm food cheaper
        Recipe.Create(ItemID.WormFood)
            .AddIngredient(ItemID.RottenChunk, 6)
            .AddIngredient(ItemID.VilePowder, 10)
            .AddTile(TileID.DemonAltar)
            .Register();
        // Make Chlorophyte bars cheaper
        Recipe.Create(ItemID.ChlorophyteBar)
            .AddIngredient(ItemID.ChlorophyteOre, 4)
            .AddTile(TileID.AdamantiteForge)
            .Register();
        // Make Make spectre bars cheaper
        Recipe.Create(ItemID.SpectreBar, 3)
            .AddIngredient(ItemID.ChlorophyteBar, 3)
            .AddIngredient(ItemID.Ectoplasm, 1)
            .AddTile(TileID.AdamantiteForge)
            .Register();

        //temp sudo terrablade (no longer used)
        //Recipe.Create(ItemID.TrueNightsEdge)
        //    .AddIngredient(ItemID.SoulofFright, 20)
        //    .AddIngredient(ItemID.SoulofMight, 20)
        //    .AddIngredient(ItemID.SoulofSight, 20)
        //    .AddIngredient(ItemID.NightsEdge)
        //    .AddIngredient(ItemID.TrueExcalibur)
        //    .AddIngredient(ItemID.BrokenHeroSword)
        //    .AddTile(TileID.MythrilAnvil)
        //    .Register();


    }
}