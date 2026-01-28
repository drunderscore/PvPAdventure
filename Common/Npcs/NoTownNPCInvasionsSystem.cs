using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

/// <summary>
/// - Prevents Town NPCs from spawning during invasions
/// - Hooks NPC.SpawnNPC via IL
/// - Forces townNPC flag to false in invasion logic
/// </summary>
public class NoTownNPCInvasionsSystem : ModSystem
{
    private static ILHook spawnNPCHook;

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.NPC).GetMethod("SpawnNPC",
            BindingFlags.Public | BindingFlags.Static);
        spawnNPCHook = new ILHook(method, RemoveTownNPCCheck);
    }

    public override void Unload()
    {
        spawnNPCHook?.Dispose();
    }

    private static void RemoveTownNPCCheck(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        try
        {
            // Find where townNPC field is loaded for invasion check
            if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(Terraria.Main).GetField("npc")),
                i => i.MatchLdloc(out _),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld(typeof(Terraria.NPC).GetField("townNPC"))
            ))
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("Found townNPC field load in invasion code");
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);

                ModContent.GetInstance<PvPAdventure>().Logger.Info("Successfully made townNPC always false for invasions");
            }
            else
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Error("Could not find townNPC field in SpawnNPC");
            }
        }
        catch (Exception e)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Error($"Error in IL edit: {e}");
        }
    }
}
internal class RemoveBanners : GlobalItem
{
    public override void OnSpawn(Item item, IEntitySource source)
    {
        if (item.createTile == TileID.Banners)
        {
            item.active = false;
            item.TurnToAir();
        }
    }

    public override void UpdateInventory(Item item, Player player)
    {
        if (item.createTile == TileID.Banners)
        {
            item.TurnToAir();
        }
    }
}
