using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;


/// <summary>
/// - Emulates the For The Worthy seed's block health via IL edit
/// </summary>
public class ForTheWorthyMiningSpeedSystem : ModSystem
{
    private static ILHook getPickaxeDamageHook;

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.Player).GetMethod("GetPickaxeDamage",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Error("Could not find GetPickaxeDamage method!");
            return;
        }

        getPickaxeDamageHook = new ILHook(method, ModifyPickaxeDamage);
    }

    public override void Unload()
    {
        getPickaxeDamageHook?.Dispose();
    }

    private static void ModifyPickaxeDamage(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        try
        {
            while (cursor.TryGotoNext(MoveType.Before, i => i.MatchRet()))
            {
                cursor.Emit(OpCodes.Conv_R4);      
                cursor.Emit(OpCodes.Ldc_R4, 1.5f); 
                cursor.Emit(OpCodes.Mul);          
                cursor.Emit(OpCodes.Conv_I4);     

                cursor.Index++;
            }

            ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Info("Successfully modified all returns in GetPickaxeDamage");
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Error($"Error in IL edit: {e}");
        }
    }
}
