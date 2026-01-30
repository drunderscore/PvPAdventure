using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace PvPAdventure.Common.WorldGenChanges.EJ;
public class ShadowKeyWorldGen : ModSystem
{
    public override void Load()
    {
        IL_WorldGen.AddBuriedChest_int_int_int_bool_int_bool_ushort += AddBuriedChest_IL;
    }

    private void AddBuriedChest_IL(ILContext il)
    {
        var cursor = new ILCursor(il);

        try
        {
            int patchCount = 0;

            while (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld(typeof(GenVars), "generatedShadowKey")))
            {
                Mod.Logger.Info($"Found generatedShadowKey field at index {cursor.Index}");

                if (cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdcI4(3) &&
                    i.Next != null &&
                    i.Next.MatchCallvirt<UnifiedRandom>("Next")))
                {
                    Mod.Logger.Info($"Found Shadow Key Next(3) call at index {cursor.Index}");

                    cursor.Remove();

                    cursor.Emit(OpCodes.Ldc_I4_1);

                    patchCount++;
                    Mod.Logger.Info($"Patched occurrence #{patchCount}");

                    cursor.Index++;
                }
                else
                {
                    cursor.Index++;
                }
            }

            if (patchCount > 0)
            {
                Mod.Logger.Info($"Successfully patched {patchCount} Shadow Key generation location(s)");
            }
            else
            {
                Mod.Logger.Error("Failed to find any Shadow Key generation patterns to patch");
            }
        }
        catch (Exception e)
        {
            Mod.Logger.Error($"Error patching AddBuriedChest: {e}");
        }
    }
}