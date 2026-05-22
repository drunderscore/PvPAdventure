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
        On_NPC.SetDefaults += OnNPCSetDefaults;
    }

    public override void Unload()
    {
        _setChatButtonsHook?.Dispose();
        _setChatButtonsHook = null;
        IL_Main.HoverOverNPCs -= PatchBoundSlimeHover;
        On_Main.TryFreeingElderSlime -= OnTryFreeingElderSlime;
        On_NPC.SetDefaults -= OnNPCSetDefaults;
        _logger = null;
    }

    private static void OnNPCSetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int type, NPCSpawnParams spawnparams)
    {
        orig(self, type, spawnparams);

        if (self.type != NPCID.BoundTownSlimeOld) return;

        self.scale = 4f;
        self.width = self.width * 4;
        self.height = self.height * 4;
    }

    private static void OnSetChatButtons(
        SetChatButtonsDelegate orig,
        ref string button,
        ref string button2)
    {
        orig(ref button, ref button2);

        if (Main.LocalPlayer.talkNPC < 0) return;
        NPC npc = Main.npc[Main.LocalPlayer.talkNPC];

        if (npc.type != NPCID.BoundTownSlimeOld) return;

        button = Language.GetTextValue("LegacyInterface.28"); // "Shop"
        button2 = "Refund Purchases";
    }

    private static bool OnTryFreeingElderSlime(On_Main.orig_TryFreeingElderSlime orig, int npcIndex)
    {
        NPC npc = Main.npc[npcIndex];
        if (npc.townNPC) return false;
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

        _logger?.Info("[ShakingChestPatches] Successfully patched BoundTownSlimeOld branch.");
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

        // Continuously delete all ground items
        for (int i = 0; i < Main.maxItems; i++)
        {
            if (Main.item[i].active)
            {
                Main.item[i].active = false;
                Main.item[i] = new Item();

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, number: i);
            }
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
        int extraHeightPixels = 18 * 3;
        NPC.NewNPC(
            Entity.GetSource_NaturalSpawn(),
            Main.spawnTileX * 16,
            (Main.spawnTileY - 3) * 16 - extraHeightPixels,
            NPCID.BoundTownSlimeOld);
    }
}