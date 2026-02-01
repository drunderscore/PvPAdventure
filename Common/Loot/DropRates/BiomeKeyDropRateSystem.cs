using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Loot.DropRates;

/// <summary>
/// - Modifies vanilla biome key drop rates <br/>
/// - Hooks ItemDropDatabase.RegisterGlobalRules via IL <br/>
/// - Replaces the 1/2500 biome key chance with 1/250 <br/>
/// - Targets only biome key drop rules <br/>
/// - Applies globally to all worlds and players <br/>
/// </summary>
public class BiomeKeyDropRateSystem : ModSystem
{
    private static ILHook globalRulesHook;

    public override void PostSetupContent()
    {
        // Apply the IL edit to change biome key drop rates from 2500 to 250
        MethodInfo method = typeof(Terraria.GameContent.ItemDropRules.ItemDropDatabase).GetMethod("RegisterGlobalRules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        globalRulesHook = new ILHook(method, BiomeKeyDropILEdit);
    }

    public override void Unload()
    {
        globalRulesHook?.Dispose();
    }

    private static void BiomeKeyDropILEdit(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        int replacedCount = 0;

        // We need to find all instances of 2500 that are used for biome keys
        // We'll look for the pattern where 2500 is loaded right before creating ItemDropWithConditionRule
        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdcI4(2500))) // Match loading the constant 2500
        {
            // Check if this is followed by the pattern that indicates it's a biome key drop rule
            // We'll look ahead to see if this leads to ItemDropWithConditionRule construction
            var nextCursor = cursor.Clone();
            bool isBiomeKey = false;

            // Look for the ItemDropWithConditionRule constructor call within a reasonable distance
            for (int j = 0; j < 250; j++) // Look ahead up to 250 instructions
            {
                if (nextCursor.TryGotoNext(MoveType.After,
                    i => i.MatchNewobj<Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule>()))
                {
                    isBiomeKey = true;
                    break;
                }
            }

            if (isBiomeKey)
            {
                // Replace the 2500 with 250
                cursor.Remove(); // Remove the ldc.i4 2500 instruction
                cursor.Emit(OpCodes.Ldc_I4, 250); // Emit ldc.i4 250 instead
                replacedCount++;

                //ModContent.GetInstance<PvPAdventure>().Logger.Info($"Replaced biome key drop rate 2500 with 250 (instance {replacedCount})");
            }
            else
            {
                // Move past this 2500 if it's not a biome key
                cursor.Index++;
            }
        }

        if (replacedCount > 0)
        {
            //ModContent.GetInstance<PvPAdventure>().Logger.Info($"Successfully changed {replacedCount} biome key drop rates from 2500 to 250");
        }
        else
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find any biome key drop rates (2500) in RegisterGlobalRules method");
        }
    }
}
