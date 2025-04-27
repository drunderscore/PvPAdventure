using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class RecipeManager : ModSystem
{
    public override void AddRecipes()
    {
        CreateDuplicateDropRecipe([
            ItemID.FlyingKnife,
            ItemID.DaedalusStormbow,
            ItemID.CrystalVileShard,
            ItemID.IlluminantHook
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.ChainGuillotines,
            ItemID.DartRifle,
            ItemID.ClingerStaff,
            ItemID.PutridScent,
            ItemID.WormHook
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.FetidBaghnakhs,
            ItemID.DartPistol,
            ItemID.SoulDrain,
            ItemID.FleshKnuckles,
            ItemID.TendonHook
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.TitanGlove,
            ItemID.MagicDagger,
            ItemID.StarCloak,
            ItemID.CrossNecklace,
            ItemID.PhilosophersStone,
            ItemID.DualHook
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.RazorbladeTyphoon,
            ItemID.Flairon,
            ItemID.BubbleGun,
            ItemID.Tsunami,
            ItemID.TempestStaff
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.BreakerBlade,
            ItemID.ClockworkAssaultRifle,
            ItemID.LaserRifle,
            ItemID.FireWhip
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.GolemFist,
            ItemID.PossessedHatchet,
            ItemID.Stynger,
            ItemID.StaffofEarth,
            ItemID.HeatRay,
            ItemID.SunStone,
            ItemID.EyeoftheGolem
        ], 3);

        CreateDuplicateDropRecipe([
            ItemID.Flairon,
            ItemID.Tsunami,
            ItemID.RazorbladeTyphoon,
            ItemID.BubbleGun,
            ItemID.TempestStaff
        ], 3);

        // Convert Cursed Flames to Ichor
        Recipe.Create(ItemID.Ichor)
            .AddIngredient(ItemID.CursedFlame)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        // Convert Ichor to Cursed Flames
        Recipe.Create(ItemID.CursedFlame)
            .AddIngredient(ItemID.Ichor)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.CrystalNinjaChestplate)
            .AddIngredient(ItemID.CrystalNinjaHelmet)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.CrystalNinjaLeggings)
            .AddIngredient(ItemID.CrystalNinjaChestplate)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.CrystalNinjaHelmet)
            .AddIngredient(ItemID.CrystalNinjaLeggings)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.GladiatorBreastplate)
            .AddIngredient(ItemID.GladiatorHelmet)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.GladiatorLeggings)
            .AddIngredient(ItemID.GladiatorBreastplate)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.GladiatorHelmet)
            .AddIngredient(ItemID.GladiatorLeggings)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.PaladinsHammer)
            .AddIngredient(ItemID.PaladinsShield)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.PaladinsShield)
            .AddIngredient(ItemID.PaladinsHammer)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.MaceWhip)
            .AddIngredient(ItemID.Keybrand)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.Keybrand)
            .AddIngredient(ItemID.MaceWhip)
            .AddTile(476) // Shimmering Pool tile ID
            .Register();

        Recipe.Create(ItemID.XenoStaff)
            .AddIngredient(ItemID.CosmicCarKey)
            .Register();

        int[] itemsToRemove = new int[]
        {
            ItemID.TrueNightsEdge,
            ItemID.MoonlordArrow
        };

        for (int i = 0; i < Main.recipe.Length; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.createItem.type != ItemID.None && itemsToRemove.Contains(recipe.createItem.type))
            {
                recipe.DisableRecipe();
            }
        }

        //temp sudo terrablade
        Recipe.Create(ItemID.TrueNightsEdge)
            .AddIngredient(ItemID.SoulofFright, 20)
            .AddIngredient(ItemID.SoulofMight, 20)
            .AddIngredient(ItemID.SoulofSight, 20)
            .AddIngredient(ItemID.NightsEdge)
            .AddIngredient(ItemID.TrueExcalibur)
            .AddIngredient(ItemID.BrokenHeroSword)
            .AddTile(TileID.MythrilAnvil)
            .Register();

        Recipe.Create(ItemID.Headstone)
            .AddIngredient(ItemID.StoneBlock, 50)
            .AddTile(TileID.HeavyWorkBench)
            .Register();
    }



    private static void CreateDuplicateDropRecipe(List<int> lootTable, int amountOfMaterial)
    {
        for (var i = 0; i < lootTable.Count; i++)

        {
            for (var j = 0; j < lootTable.Count; j++)
            {
                if (j == i)
                    continue;

                Recipe.Create(lootTable[i])
                    .AddIngredient(lootTable[j], amountOfMaterial)
                    .DisableDecraft()
                    .Register();
            }
        }
    }
}
