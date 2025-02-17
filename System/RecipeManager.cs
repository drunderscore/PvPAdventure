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
            [ItemID.BreakerBlade, ItemID.ClockworkAssaultRifle, ItemID.LaserRifle, ItemID.FireWhip],
            [
                ItemID.GolemFist, ItemID.PossessedHatchet, ItemID.Stynger, ItemID.StaffofEarth, ItemID.HeatRay,
                ItemID.SunStone, ItemID.EyeoftheGolem
            ],
            [ItemID.Flairon, ItemID.Tsunami, ItemID.RazorbladeTyphoon, ItemID.BubbleGun, ItemID.TempestStaff]
        ];

        public override void AddRecipes()
        {
            foreach (var lootTable in _lootTables)
                CreateDuplicateDropRecipe(lootTable, 3);
        }

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
    }
}