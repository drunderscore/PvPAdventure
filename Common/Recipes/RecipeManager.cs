using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Loot;

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