using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// Removes all potions from Pot loot tables.
/// </summary>
public class RemovePotPotions : ModSystem
{
    private static ILHook? _hook;

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(WorldGen)
            .GetMethod("SpawnThingsFromPot", BindingFlags.NonPublic | BindingFlags.Static)!;
        _hook = new ILHook(method, ILEdit);
    }

    public override void Unload() => _hook?.Dispose();

    private static void ILEdit(ILContext il)
    {
        var cursor = new ILCursor(il);
        {
            ILLabel elseLabel = null;
            cursor.GotoNext(
                i => i.MatchLdcI4(45),
                i => i.MatchCallvirt(out _),
                i => i.MatchBrtrue(out elseLabel)
            );

            cursor.Goto(0);
            cursor.GotoNext(
                i => i.MatchLdcI4(45),
                i => i.MatchCallvirt(out _),
                i => i.MatchBrfalse(out _)
            );

            cursor.Index -= 1;

            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Br, elseLabel);
        }

        cursor.Goto(0);
        if (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchCall(out var mr) && mr.Name == "get_rand",
            i => i.MatchLdcI4(30),
            i => i.MatchCallvirt(out var mr2) && mr2.Name == "Next",
            i => i.MatchBrtrue(out _),
            i => i.MatchLdarg(0),
            i => i.MatchLdarg(1),
            i => i.MatchCall(out var mr3) && mr3.Name == "GetItemSource_FromTileBreak",
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(2997)))
        {
            for (int n = 0; n < 24; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }
    }
}