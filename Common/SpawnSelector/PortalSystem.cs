using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Content.NPCs;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Server-authoritative portal registry and helpers.
/// The portal's actual multiplayer state lives on <see cref="PortalNPC"/>.
/// </summary>
public sealed class PortalSystem : ModSystem
{
    public static int PortalMaxHealth => NPC.downedPlantBoss ? 420 : Main.hardMode ? 69 : 27;
    public const float PortalUseRangeTiles = 8f;
    public const float PortalUseRangeWorld = PortalUseRangeTiles * 16f;
    public static int PortalCreateAnimationTicks =>
        Math.Max(0, ModContent.GetInstance<ServerConfig>().AdventureMirrorRecallSeconds * 60);

    public static bool HasPortal(Player player) => TryGetPortalNpc(player, out _, out _);

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos)
    {
        if (TryGetPortal(player, out worldPos, out _, out _, out _))
            return true;

        worldPos = default;
        return false;
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health)
    {
        return TryGetPortal(player, out worldPos, out health, out _, out _);
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health, out int createTicksRemaining)
    {
        return TryGetPortal(player, out worldPos, out health, out createTicksRemaining, out _);
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health, out int createTicksRemaining, out int maxHealth)
    {
        worldPos = default;
        health = 0;
        createTicksRemaining = 0;
        maxHealth = 0;

        if (!TryGetPortalNpc(player, out NPC npc, out PortalNPC portal))
            return false;

        worldPos = portal.WorldPosition;
        health = npc.life;
        createTicksRemaining = portal.CreateTicksRemaining;
        maxHealth = npc.lifeMax;
        return true;
    }

    public static IEnumerable<NPC> EnumeratePortalNpcs()
    {
        int type = ModContent.NPCType<PortalNPC>();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.active == true && npc.type == type && npc.ModNPC is PortalNPC)
                yield return npc;
        }
    }

    public static bool CreatePortalAtPosition(Player player, Vector2 requestedPosition)
    {
        return TryCreatePortalAtPosition(player, requestedPosition, out _);
    }

    public static bool TryCreatePortalAtPosition(Player player, Vector2 requestedPosition, out string reason)
    {
        if (player?.active != true || Main.netMode == NetmodeID.MultiplayerClient)
        {
            reason = $"blocked before create: active={player?.active == true}, netMode={Main.netMode}";
            return false;
        }

        return TryCreatePortalServer(player, requestedPosition, announce: true, out reason);
    }

    private static bool TryCreatePortalServer(Player player, Vector2 worldPos, bool announce, out string reason)
    {
        if (player?.active != true || Main.netMode == NetmodeID.MultiplayerClient)
        {
            reason = $"blocked server create: active={player?.active == true}, netMode={Main.netMode}";
            return false;
        }

        if (!CanCreatePortal(player, out string blockedReason))
        {
            reason = blockedReason;
            Log.Chat($"[Portal] Failed to create portal for {player.name}: {reason}");
            return false;
        }

        RemovePortalsForOwner(player.whoAmI, silent: true);

        int npcIndex = NPC.NewNPC(
            player.GetSource_FromThis("AdventureMirrorPortal"),
            (int)(worldPos.X - PortalNPC.PortalWidth * 0.5f),
            (int)(worldPos.Y - PortalNPC.PortalHeight),
            ModContent.NPCType<PortalNPC>(),
            ai0: player.whoAmI,
            ai1: player.team,
            ai2: PortalMaxHealth,
            ai3: 0
        );

        if ((uint)npcIndex >= Main.maxNPCs || Main.npc[npcIndex].ModNPC is not PortalNPC portal)
        {
            reason = $"NPC.NewNPC failed at {worldPos}, npcIndex={npcIndex}";
            Log.Chat($"[Portal] Failed to create portal for {player.name}: {reason}");
            return false;
        }

        NPC npc = Main.npc[npcIndex];
        portal.Initialize(player, worldPos);
        npc.netUpdate = true;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);

        InvalidateSpawnRegionCaches();

        if (announce)
            SendPortalCreatedMessages(player, worldPos);

        reason = $"npc={npcIndex}, owner={player.name}, team={player.team}, hp={npc.life}/{npc.lifeMax}, pos={worldPos}";
        Log.Chat($"[Portal] Successfully created portal: {reason}");
        return true;
    }

    private static bool CanCreatePortal(Player player, out string blockedReason)
    {
        blockedReason = string.Empty;

        if (player.ghost)
        {
            blockedReason = "owner is a ghost";
            return false;
        }

        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
        {
            blockedReason = "game is not playing";
            return false;
        }

        return true;
    }

    public static void ClearPortal(Player player)
    {
        if (player?.active != true)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ClearLocalPortalSelection(player.whoAmI);
            return;
        }

        RemovePortalsForOwner(player.whoAmI, silent: true);
        ClearLocalPortalSelection(player.whoAmI);
    }

    internal static void RemovePortalNpc(NPC npc, bool silent)
    {
        if (npc == null || !npc.active)
            return;

        int ownerIndex = npc.ModNPC is PortalNPC portal ? portal.OwnerIndex : -1;

        npc.active = false;
        npc.life = 0;
        npc.netSkip = -1;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);

        InvalidateSpawnRegionCaches();
        ClearLocalPortalSelection(ownerIndex);
    }

    private static void RemovePortalsForOwner(int ownerIndex, bool silent)
    {
        foreach (NPC npc in EnumeratePortalNpcs())
        {
            if (npc.ModNPC is PortalNPC portal && portal.OwnerIndex == ownerIndex)
                RemovePortalNpc(npc, silent);
        }
    }

    internal static void HandlePortalKilled(NPC npc, PortalNPC portal)
    {
        if (npc == null || portal == null)
            return;

        InvalidateSpawnRegionCaches();
        ClearLocalPortalSelection(portal.OwnerIndex);

        if (!TryGetActivePlayer(portal.OwnerIndex, out Player owner))
        {
            Log.Debug($"[Portal] destroyed npc={npc.whoAmI}, but owner index {portal.OwnerIndex} was not active");
            return;
        }

        Color color = GetPortalTeamTextColor(owner);
        TeleportChat.SendSystemTeamMessage(owner, $"{owner.name}'s portal has been destroyed.", color);
        Log.Debug($"[Portal] destroyed owner={owner.name} npc={npc.whoAmI}");
    }

    public static Rectangle GetPortalHitbox(Vector2 worldPos) =>
        new((int)worldPos.X - PortalNPC.PortalWidth / 2, (int)worldPos.Y - PortalNPC.PortalHeight, PortalNPC.PortalWidth, PortalNPC.PortalHeight);

    public static Vector2 GetPortalTopLeft(Vector2 worldPos) =>
        worldPos - new Vector2(PortalNPC.PortalWidth * 0.5f, PortalNPC.PortalHeight);

    public static bool IsWithinPortalUseRange(Player player, Vector2 worldPos)
    {
        return player?.active == true &&
               Vector2.DistanceSquared(player.Center, worldPos) <= PortalUseRangeWorld * PortalUseRangeWorld;
    }

    internal static bool CanPlayerDamagePortal(Player attacker, int ownerIndex)
    {
        if (attacker?.active != true)
            return false;

        if (!TryGetActivePlayer(ownerIndex, out Player owner))
            return false;

        return attacker.team != 0 && owner.team != 0 && attacker.team != owner.team;
    }

    public static void PlayPortalFx(Vector2 worldPos, bool killed, int damage = 0)
    {
        if (Main.dedServ)
            return;

        if (damage > 0)
            CombatText.NewText(GetPortalHitbox(worldPos), CombatText.DamagedHostile, damage);

        if (!killed)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4, worldPos);
            return;
        }

        SoundEngine.PlaySound(SoundID.NPCDeath6, worldPos);

        for (int i = 0; i < 28; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
            Dust.NewDustPerfect(worldPos + Main.rand.NextVector2Circular(24f, 36f), DustID.MagicMirror, velocity, 120, Color.White, Main.rand.NextFloat(1.1f, 1.8f));
        }
    }

    internal static Color GetPortalTeamTextColor(Player player)
    {
        if (player != null && player.team >= 0 && player.team < Main.teamColor.Length)
            return Main.teamColor[player.team];

        Log.Debug($"Failed to resolve portal team text color for player={player?.name ?? "<null>"} team={player?.team ?? -1}; using white fallback");
        return Color.White;
    }

    internal static string GetOwnPortalMessage(Player player, string biome, int distance)
    {
        return $"You opened a portal in {biome}";
    }

    internal static string GetPortalMessage(Player player, string biome, int distance)
    {
        return $"{player.name} opened a portal in {biome} ({distance} tiles away)";
    }

    private static void SendPortalCreatedMessages(Player owner, Vector2 portalWorldPos)
    {
        if (owner?.active != true)
            return;

        string biome = PlayerStats.GetBiomeText(owner);
        Color color = GetPortalTeamTextColor(owner);

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            int distance = GetDistanceTiles(Main.LocalPlayer ?? owner, portalWorldPos);
            Main.NewText(GetOwnPortalMessage(owner, biome, distance), color);
            return;
        }

        if (Main.netMode != NetmodeID.Server)
            return;

        if (owner.team == 0)
        {
            int distance = GetDistanceTiles(owner, portalWorldPos);
            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(GetOwnPortalMessage(owner, biome, distance)), color, owner.whoAmI);
            return;
        }

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player viewer = Main.player[i];
            if (viewer?.active != true || viewer.team != owner.team)
                continue;

            int distance = GetDistanceTiles(viewer, portalWorldPos);
            string text = i == owner.whoAmI
                ? GetOwnPortalMessage(owner, biome, distance)
                : GetPortalMessage(owner, biome, distance);

            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(ChatPrefixFormatter.TeamChannelMarker + text), color, i);
        }
    }

    private static int GetDistanceTiles(Player viewer, Vector2 worldPos)
    {
        if (viewer?.active != true)
            return 0;

        return Math.Max(0, (int)Math.Round(Vector2.Distance(viewer.Center, worldPos) / 16f));
    }

    private static bool TryGetPortalNpc(Player player, out NPC npc, out PortalNPC portal)
    {
        npc = null;
        portal = null;

        if (player?.active != true)
            return false;

        foreach (NPC candidate in EnumeratePortalNpcs())
        {
            if (candidate.ModNPC is not PortalNPC candidatePortal || candidatePortal.OwnerIndex != player.whoAmI)
                continue;

            npc = candidate;
            portal = candidatePortal;
            return true;
        }

        return false;
    }

    private static bool TryGetActivePlayer(int playerIndex, out Player player)
    {
        player = playerIndex >= 0 && playerIndex < Main.maxPlayers ? Main.player[playerIndex] : null;
        return player?.active == true;
    }

    internal static void InvalidateSpawnRegionCaches()
    {
        SpawnPlayer.InvalidateSpawnRegionCaches();
    }

    private static void ClearLocalPortalSelection(int ownerIndex)
    {
        if (Main.dedServ)
            return;

        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        if (sp.SelectedType == SpawnType.MyPortal && ownerIndex == local.whoAmI)
            sp.ClearSelection();

        if (sp.SelectedType == SpawnType.TeammatePortal && sp.SelectedPlayerIndex == ownerIndex)
            sp.ClearSelection();
    }

    public override void OnWorldLoad()
    {
        ClearAllPortalNpcs();
    }

    public override void OnWorldUnload()
    {
        ClearAllPortalNpcs();
    }

    private static void ClearAllPortalNpcs()
    {
        foreach (NPC npc in EnumeratePortalNpcs())
            RemovePortalNpc(npc, silent: true);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        if (sp.SelectedType == SpawnType.MyPortal && !HasPortal(local))
            sp.ClearSelection();
        else if (sp.SelectedType == SpawnType.TeammatePortal && !SpawnPlayer.IsValidTeammatePortalIndex(local, sp.SelectedPlayerIndex))
            sp.ClearSelection();
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        PortalDrawer.DrawAllPortals(Main.spriteBatch);
        Main.spriteBatch.End();
    }

    public override void PostUpdateInput()
    {
        if (Main.dedServ)
            return;

        if (!Main.mouseRight || !Main.mouseRightRelease)
            return;

        if (Main.LocalPlayer == null || !Main.LocalPlayer.active)
            return;

        if (Main.LocalPlayer.mouseInterface || Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput)
            return;

        if (!TryGetHoveredPortal(Main.MouseWorld, out _))
            return;

        OpenFullscreenMap();
        Main.mouseRightRelease = false;
    }

    private static bool TryGetHoveredPortal(Vector2 mouseWorld, out int ownerIndex)
    {
        ownerIndex = -1;
        Point mousePoint = mouseWorld.ToPoint();

        foreach (NPC npc in EnumeratePortalNpcs())
        {
            if (npc.ModNPC is not PortalNPC portal)
                continue;

            if (!GetPortalHitbox(portal.WorldPosition).Contains(mousePoint))
                continue;

            ownerIndex = portal.OwnerIndex;
            return true;
        }

        return false;
    }

    private static void OpenFullscreenMap()
    {
        Main.playerInventory = false;
        Main.LocalPlayer.talkNPC = -1;
        Main.npcChatCornerItem = 0;
        Main.mapFullscreen = true;
        Main.resetMapFull = true;

        SoundEngine.PlaySound(SoundID.MenuOpen);
    }
}
