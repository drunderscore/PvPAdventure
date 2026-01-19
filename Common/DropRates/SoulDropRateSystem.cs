using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.ModLoader;

namespace PvPAdventure.Common.DropRates;

public class SoulDropRateSystem : ModSystem
{
    private static ILHook globalRulesHook;

    private const int NewSoulDropRate = 3; // denominator

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.GameContent.ItemDropRules.ItemDropDatabase).GetMethod("RegisterGlobalRules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        globalRulesHook = new ILHook(method, SoulDropILEdit);
    }

    public override void Unload()
    {
        globalRulesHook?.Dispose();
    }

    private static void SoulDropILEdit(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        int replacedCount = 0;


        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdcI4(5)))
        {

            var nextCursor = cursor.Clone();
            bool isSoulDrop = false;


            for (int j = 0; j < 100; j++)
            {
                if (nextCursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(520) || i.MatchLdcI4(521)))
                {
                    isSoulDrop = true;
                    break;
                }
            }

            if (isSoulDrop)
            {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldc_I4, NewSoulDropRate);
                replacedCount++;
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Replaced soul drop rate 5 with {NewSoulDropRate} (instance {replacedCount})");
            }
            else
            {
                cursor.Index++;
            }
        }

        if (replacedCount > 0)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Info($"Successfully changed {replacedCount} soul drop rates from 5 to {NewSoulDropRate}");
        }
        else
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find any soul drop rates (5) in RegisterGlobalRules method");
        }
    }
}
