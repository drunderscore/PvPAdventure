using log4net;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using PvPAdventure.Common.GameTimer;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

[Autoload(Side = ModSide.Both)]
public class ShakingChestSystem : ModSystem
{
    private const int RespawnCheckInterval = 300;
    private int _respawnCheckTimer = 0;
    private static ILog _logger;
    private static Hook _setChatButtonsHook;

    private delegate void SetChatButtonsDelegate(ref string button, ref string button2);

    public override void Load()
    {
        _logger = Mod.Logger;

        var method = typeof(NPCLoader).GetMethod(
            "SetChatButtons",
            BindingFlags.Public | BindingFlags.Static);

        _setChatButtonsHook = new Hook(method, OnSetChatButtons);

        IL_Main.HoverOverNPCs += PatchBoundSlimeHover;
        On_Main.TryFreeingElderSlime += OnTryFreeingElderSlime;
    }

    public override void Unload()
    {
        _setChatButtonsHook?.Dispose();
        _setChatButtonsHook = null;
        IL_Main.HoverOverNPCs -= PatchBoundSlimeHover;
        On_Main.TryFreeingElderSlime -= OnTryFreeingElderSlime;
        _logger = null;
    }

    private static void OnSetChatButtons(
        SetChatButtonsDelegate orig,
        ref string button,
        ref string button2)
    {
        orig(ref button, ref button2);

        if (Main.LocalPlayer.talkNPC < 0) return;
        NPC npc = Main.npc[Main.LocalPlayer.talkNPC];

        if (npc.type == NPCID.BoundTownSlimeOld)
            button = Language.GetTextValue("LegacyInterface.28"); // "Shop"
    }

    private static bool OnTryFreeingElderSlime(On_Main.orig_TryFreeingElderSlime orig, int npcIndex)
    {
        NPC npc = Main.npc[npcIndex];
        if (npc.townNPC)
            return false;
        return orig(npcIndex);
    }

    private static void PatchBoundSlimeHover(ILContext il)
    {
        var c = new ILCursor(il);

        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchLdfld<NPC>(nameof(NPC.type)),
            i => i.MatchLdcI4(NPCID.BoundTownSlimeOld)))
        {
            _logger?.Warn("[ShakingChestPatches] Failed to find BoundTownSlimeOld ldc.i4 in HoverOverNPCs.");
            return;
        }

        c.GotoNext(i => i.MatchLdcI4(NPCID.BoundTownSlimeOld));
        c.Next.Operand = -1;
    }

    public override void OnWorldLoad()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Waiting)
            EnsureShakingChestExists();
    }

    public override void PostWorldGen()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        SpawnShakingChest();
    }

    public override void PostUpdateTime()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Waiting)
        {
            _respawnCheckTimer = 0;
            return;
        }

        if (++_respawnCheckTimer < RespawnCheckInterval) return;

        _respawnCheckTimer = 0;
        EnsureShakingChestExists();
    }

    private static void EnsureShakingChestExists()
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type == NPCID.BoundTownSlimeOld)
                return;
        }
        SpawnShakingChest();
    }

    private static void SpawnShakingChest()
    {
        NPC.NewNPC(
            Entity.GetSource_NaturalSpawn(),
            Main.spawnTileX * 16,
            (Main.spawnTileY - 3) * 16,
            NPCID.BoundTownSlimeOld);
    }
}