using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.WorldGenChanges.EJ;

/// <summary>
/// Multiplies herb growth rate and "sprout" rate (growalch)
/// </summary>
public class HerbMultiplierSystem : ModSystem
{
    private static ILHook growAlchHook;

    private static readonly int[] growthDivisors = { 50, 2, 30 };

    public static double growthMultiplier = 3.0; 
    public static double spawnMultiplier = 2; 
    private static int ExtraCallsPerTick => Math.Max(0, (int)Math.Round(spawnMultiplier) - 1);

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.WorldGen).GetMethod("GrowAlch",
            BindingFlags.Public | BindingFlags.Static);
        growAlchHook = new ILHook(method, GrowAlchILEdit);
    }

    public override void Unload()
    {
        growAlchHook?.Dispose();
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == 1 || Main.gameMenu)
            return;

        for (int i = 0; i < ExtraCallsPerTick; i++)
        {
            WorldGen.PlantAlch();
        }
    }

    private static void GrowAlchILEdit(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        int value = 0;
        int patchedRolls = 0;

        try
        {
            while (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdcI4(out value) && Array.IndexOf(growthDivisors, value) >= 0,
                i => i.MatchCallOrCallvirt(out Mono.Cecil.MethodReference m) && m.Name == "Next" && m.Parameters.Count == 1))
            {
                int newValue = Math.Max(1, (int)Math.Round(value / growthMultiplier));
                cursor.Remove();
                cursor.Emit(OpCodes.Ldc_I4, newValue);
                patchedRolls++;
                cursor.Index++;
            }

            ModContent.GetInstance<PvPAdventure>().Logger.Info(
                $"HerbMultiplierSystem: sped up {patchedRolls} growth rolls (x{growthMultiplier}), spawn rate x{spawnMultiplier}");
        }
        catch (Exception e)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Error($"Error patching GrowAlch: {e}");
        }
    }
}