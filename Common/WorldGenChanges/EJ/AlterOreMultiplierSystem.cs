using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace PvPAdventure.Common.WorldGenChanges.EJ;

/// <summary>
/// - Increases Hardmode ore generation from Demon Altars
/// - Hooks WorldGen.SmashAltar via IL
/// - Multiplies ore yield by a fixed factor
/// </summary>
public class AltarOreMultiplierSystem : ModSystem
{
    private static ILHook altarHook;

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.WorldGen).GetMethod("SmashAltar",
            BindingFlags.Public | BindingFlags.Static);
        altarHook = new ILHook(method, AltarOreILEdit);
    }

    public override void Unload()
    {
        altarHook?.Dispose();
    }

    private static void AltarOreILEdit(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        try
        {
            if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(2),      // Load num3
                i => i.MatchLdloc(1),      // Load num2
                i => i.MatchConvR8(),      // Convert num2 to double
                i => i.MatchDiv(),         // Divide
                i => i.MatchStloc(2)       // Store back to num3
            ))
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("Found num3 /= num2 calculation");

                double multiplier = 2.0; // THIS NUMBER MULTIPLIES IT

                cursor.Emit(OpCodes.Ldloc_2);                 // Load num3
                cursor.Emit(OpCodes.Ldc_R8, multiplier);      // Load multiplier
                cursor.Emit(OpCodes.Mul);                     // Multiply
                cursor.Emit(OpCodes.Stloc_2);                 // Store back to num3

                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Successfully patched SmashAltar to increase ore generation by {multiplier}x");
                return;
            }

            ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find num3 calculation in SmashAltar method");
        }
        catch (Exception e)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Error($"Error patching SmashAltar: {e}");
        }
    }
}
