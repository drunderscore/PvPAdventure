using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;


/// <summary>
/// - Increases pickaxe mining damage globally <br/>
/// - Hooks Player.GetPickaxeDamage via IL <br/>
/// - Applies a fixed 1.5× mining multiplier <br/>
/// - Emulates For the Worthy mining behavior <br/>
/// - Affects all players and pickaxes <br/>
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
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchRet()
            ))
            {
                //ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Info("Found return in GetPickaxeDamage");

                cursor.Emit(OpCodes.Ldc_R4, 1.5f); //for the worthy mining speed
                cursor.Emit(OpCodes.Mul);
                cursor.Emit(OpCodes.Conv_I4);

                //ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Info("Successfully modified GetPickaxeDamage to multiply damage by 1.5");
            }
            else
            {
                ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Error("Could not find return statement in GetPickaxeDamage");
            }
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ForTheWorthyMiningSpeedSystem>().Mod.Logger.Error($"Error in IL edit: {e}");
        }
    }
}
