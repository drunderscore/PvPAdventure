using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// Makes the Cultists and Martian Probes always able to spawn, even if danger is present .
/// </summary>
public class MakeCultistsAndProbesAlwaysSpawn : ModSystem
{
    private static ILHook? _checkRitualHook;
    private static ILHook? _updateTimeHook;
    private static ILHook? _spawnMobsHook;

    public override void PostSetupContent()
    {
        var logger = ModContent.GetInstance<PvPAdventure>().Logger;

        Type cultistRitualType = typeof(Main).Assembly.GetType("Terraria.GameContent.Events.CultistRitual")!;

        MethodInfo checkRitual = cultistRitualType.GetMethod("CheckRitual",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        MethodInfo updateTime = cultistRitualType.GetMethod("UpdateTime",
            BindingFlags.Public | BindingFlags.Static)!;

        _checkRitualHook = new ILHook(checkRitual, CheckRitualILEdit);
        _updateTimeHook = new ILHook(updateTime, UpdateTimeILEdit);

        MethodInfo spawnNPC = typeof(NPC).GetMethod("SpawnNPC",
            BindingFlags.Public | BindingFlags.Static)!;
        _spawnMobsHook = new ILHook(spawnNPC, SpawnMobsILEdit);
    }

    public override void Unload()
    {
        _checkRitualHook?.Dispose();
        _updateTimeHook?.Dispose();
        _spawnMobsHook?.Dispose();
    }
    private static void UpdateTimeILEdit(ILContext il)
    {
        var logger = ModContent.GetInstance<PvPAdventure>().Logger;

        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdcI4(0),
            i => i.MatchLdcI4(0),
            i => i.MatchCall(out var mr) && mr.Name == "AnyDanger",
            i => i.MatchBrfalse(out _),
            i => i.MatchLdsfld(out _),
            i => i.MatchLdcR8(6),
            i => i.MatchMul(),
            i => i.MatchStsfld(out _),
            i => i.MatchRet()))
        {
            for (int n = 0; n < 9; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }
        else
        {
            logger.Error("[MakeCultistsAndProbesAlwaysSpawn] Failed to find AnyDanger check.");
        }
    }
    private static void CheckRitualILEdit(ILContext il)
    {
        var logger = ModContent.GetInstance<PvPAdventure>().Logger;

        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(7),
            i => i.MatchBlt(out _),
            i => i.MatchLdsflda(out _),
            i => i.MatchLdarg(0),
            i => i.MatchLdarg(1),
            i => i.MatchLdcI4(7),
            i => i.MatchSub(),
            i => i.MatchCall(out _),
            i => i.MatchCall(out _),
            i => i.MatchBrfalse(out _),
            i => i.MatchLdcI4(0),
            i => i.MatchRet()))
        {
            for (int n = 0; n < 13; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }
        else
        {
            logger.Error("[MakeCultistsAndProbesAlwaysSpawn] Failed to find solid tile check.");
        }
    }

    private static void SpawnMobsILEdit(ILContext il)
    {
        var logger = ModContent.GetInstance<PvPAdventure>().Logger;
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdcI4(0),
            i => i.MatchLdcI4(0),
            i => i.MatchCall(out var mr) && mr.Name == "AnyDanger",
            i => i.MatchBrfalse(out _),
            i => i.MatchLdcI4(0),
            i => i.MatchStloc(out _)))
        {
            for (int n = 0; n < 6; n++)
            {
                cursor.Next!.OpCode = OpCodes.Nop;
                cursor.Next!.Operand = null;
                cursor.Index++;
            }
        }
        else
        {
            logger.Error("[MakeCultistsAndProbesAlwaysSpawn] Failed to find probe AnyDanger check.");
        }
    }
}