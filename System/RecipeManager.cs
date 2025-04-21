using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System
{
    [Autoload]
    public class RecipeManager : ModSystem
    {
        private readonly List<List<int>> _lootTables =
        [
            [ItemID.FlyingKnife, ItemID.DaedalusStormbow, ItemID.CrystalVileShard, ItemID.IlluminantHook],
            [ItemID.ChainGuillotines, ItemID.DartRifle, ItemID.ClingerStaff, ItemID.PutridScent, ItemID.WormHook],
            [ItemID.FetidBaghnakhs, ItemID.DartPistol, ItemID.SoulDrain, ItemID.FleshKnuckles, ItemID.TendonHook],
            [ItemID.TitanGlove, ItemID.MagicDagger, ItemID.StarCloak, ItemID.CrossNecklace, ItemID.PhilosophersStone, ItemID.DualHook],
            [ItemID.RazorbladeTyphoon, ItemID.Flairon, ItemID.BubbleGun, ItemID.Tsunami, ItemID.TempestStaff],
            [ItemID.BreakerBlade, ItemID.ClockworkAssaultRifle, ItemID.LaserRifle, ItemID.FireWhip],
            [
                ItemID.GolemFist, ItemID.PossessedHatchet, ItemID.Stynger, ItemID.StaffofEarth, ItemID.HeatRay,
                ItemID.SunStone, ItemID.EyeoftheGolem
            ],
            [ItemID.Flairon, ItemID.Tsunami, ItemID.RazorbladeTyphoon, ItemID.BubbleGun, ItemID.TempestStaff]
        ];

        private static void CreateDuplicateDropRecipe(List<int> lootTable, int amountOfMaterial)
        {
            for (int i = 0; i < lootTable.Count; i++)
            {
                for (int j = 0; j < lootTable.Count; j++)
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

        public override void AddRecipes()
        {
            //adds the multi boss drop recipies (placeholder code)
            foreach (var lootTable in _lootTables)
                CreateDuplicateDropRecipe(lootTable, 3);
           
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

            Recipe.Create(ItemID.Headstone)
                .AddIngredient(ItemID.StoneBlock, 50)
                .AddTile(TileID.HeavyWorkBench) 
                .Register();

            //placeholder code before proper item replacments
            Recipe.Create(ItemID.Tabi)
                .AddIngredient(ItemID.BlackBelt)
                .Register();

            Recipe.Create(ItemID.AncientChisel)
                .AddIngredient(ItemID.MagicConch)
                .Register();

            Recipe.Create(ItemID.XenoStaff)
                .AddIngredient(ItemID.CosmicCarKey)
                .Register();

            Recipe.Create(ItemID.CloudinaBottle)
                .AddIngredient(ItemID.MagicMirror)
                .Register();

            Recipe.Create(ItemID.FeralClaws)
                .AddIngredient(ItemID.Seaweed)
                .Register();

            Recipe.Create(ItemID.BlizzardinaBottle)
                .AddIngredient(ItemID.Fish)
                .Register();

        }
    }
}
