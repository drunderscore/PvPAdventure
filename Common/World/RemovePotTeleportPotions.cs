using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// Removes Recall Potions, Wormhole Potions, Potions of Return, Featherfall Potions, and Gravitation Potions from Pot loot tables. It is currently suspected that the featherfall potion and gravitation potion portions do not work.
/// </summary>
public class RemovePotTeleportPotions : ModSystem
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
        // Recall Potions
        var cursor = new ILCursor(il);
        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchCall(out var mr) && mr.Name == "GetItemSource_FromTileBreak",
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(2350)))
        {
            cursor.Index -= 2;
            for (int n = 0; n < 23; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }

        // Wormhole Potions
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

        // Return Potions
        cursor.Goto(0);
        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchCall(out var mr) && mr.Name == "GetItemSource_FromTileBreak",
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(4870)))
        {
            cursor.Index -= 2;
            for (int n = 0; n < 20; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }

        // Gravitation Potions
        cursor.Goto(0);
        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdarg(1),
            i => i.MatchCall(out var mr) && mr.Name == "GetItemSource_FromTileBreak",
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(303)))
        {
            for (int n = 0; n < 19; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }

        // Featherfall Potions
        cursor.Goto(0);
        while (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdarg(1),
            i => i.MatchCall(out var mr) && mr.Name == "GetItemSource_FromTileBreak",
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(16),
            i => i.MatchMul(),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(16),
            i => i.MatchLdcI4(291)))
        {
            for (int n = 0; n < 19; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }
    }
}