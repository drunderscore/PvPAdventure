using Terraria;
using Terraria.ModLoader;
using MonoMod.Cil;

namespace LifeFruitPatcher
{
    public class LifeFruitPatcherMod : ModSystem
    {
        public override void Load()
        {
            // Hook into the IL of WorldGen.UpdateWorld_GrassGrowth
            IL_WorldGen.UpdateWorld_GrassGrowth += ModifyGrassGrowthIL;
        }

        private void ModifyGrassGrowthIL(ILContext il)
        {
            var cursor = new ILCursor(il);

            // Patch 1: Change maxValue2 (non-expert) from 40 → 2
            cursor.GotoNext(MoveType.After,
                x => x.MatchLdcI4(40));
                cursor.Prev.Operand = 2;
            

            
           
            cursor.Index = 0;

            // Patch 2: Change num7 (radius) from 60 → 2
            cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(60));
                cursor.Prev.Operand = 2;
            
           
        }
    }
}