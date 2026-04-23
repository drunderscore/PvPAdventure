using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

internal enum PlayerMode : byte
{
    Player,
    Spectator
}

internal enum SpectatorTargetKind : byte
{
    None,
    Player,
    NPC
}

internal sealed class SpectatorSystem : ModSystem
{
    internal static readonly Dictionary<int, PlayerMode> Modes = [];

    private static Player LocalPlayerOrNull()
    {
        Player local = Main.LocalPlayer;
        return local?.active == true ? local : null;
    }

    private static Player LocalSpectatorOrNull()
    {
        Player local = LocalPlayerOrNull();
        return IsInSpectateMode(local) ? local : null;
    }

    private static SpectatorPlayer LocalSpectatorPlayerOrNull()
    {
        Player local = LocalPlayerOrNull();
        return local?.GetModPlayer<SpectatorPlayer>();
    }

    private static void ApplyLocalModeEffects(int slot, PlayerMode mode)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Player local = LocalPlayerOrNull();
        if (local?.whoAmI != slot)
            return;

        if (mode == PlayerMode.Spectator)
        {
            local.ghost = true;
            Main.playerInventory = false;
            return;
        }

        local.ghost = false;
        local.GetModPlayer<SpectatorPlayer>().ClearTarget();
    }

    private static int NextIndex(List<int> list, int current, bool forward)
    {
        if (list.Count == 0)
            return -1;

        int index = list.IndexOf(current);
        if (index < 0)
            return forward ? 0 : list.Count - 1;

        return forward ? (index + 1) % list.Count : (index - 1 + list.Count) % list.Count;
    }

    internal static PlayerMode GetJoinDefaultMode()
    {
        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();
        return config.ForcePlayersToBeSpectatorsWhenJoining ? PlayerMode.Spectator : PlayerMode.Player;
    }

    public static PlayerMode GetMode(int slot) => Modes.TryGetValue(slot, out PlayerMode mode) ? mode : PlayerMode.Player;

    public static bool IsInSpectateMode(Player player) => player?.active == true && GetMode(player.whoAmI) == PlayerMode.Spectator;

    public static bool IsInPlayerMode(Player player) => player?.active == true && GetMode(player.whoAmI) == PlayerMode.Player;

    public static void RequestFullSync()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestFullSync();
    }

    public static void RequestSetLocalMode(PlayerMode mode)
    {
        if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            return;

        SetModeLocal(Main.myPlayer, mode);

        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestSetMode(Main.myPlayer, mode);
    }

    public static void RequestSetMode(int slot, PlayerMode mode)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (slot == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            if (mode == PlayerMode.Spectator)
                SpectatorUISystem.EnterSpectateMode();
            else
                SpectatorUISystem.EnterPlayerMode();

            return;
        }

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            SetModeLocal(slot, mode);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestSetMode(slot, mode);
    }

    public static void RequestSetPlayerMode(int slot) => RequestSetMode(slot, PlayerMode.Player);

    public static void RequestSetSpectatorMode(int slot) => RequestSetMode(slot, PlayerMode.Spectator);

    public static void SetModeServer(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.Server || slot < 0 || slot >= Main.maxPlayers)
            return;

        Player player = Main.player[slot];
        if (!player.active || GetMode(slot) == mode)
            return;

        Modes[slot] = mode;
        SpectatorNetHandler.SendMode(slot, mode);
    }

    public static void SetModeLocal(int slot, PlayerMode mode)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        Modes[slot] = mode;
        ApplyLocalModeEffects(slot, mode);
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> list = [];
        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i].active && i != exclude && IsInPlayerMode(Main.player[i]) && !Main.player[i].ghost)
                list.Add(i);

        return list;
    }

    public static List<int> GetNPCTargets()
    {
        List<int> list = [];
        for (int i = 0; i < Main.maxNPCs; i++)
            if (Main.npc[i]?.active == true)
                list.Add(i);

        return list;
    }

    public static void SetPlayerTarget(int playerIndex)
    {
        // UI helper: callers may queue a target before the local client receives mode sync.
        SpectatorPlayer modPlayer = LocalSpectatorPlayerOrNull();
        if (modPlayer == null && Main.netMode != NetmodeID.MultiplayerClient)
            return;

        modPlayer ??= LocalSpectatorPlayerOrNull() ?? LocalPlayerOrNull()?.GetModPlayer<SpectatorPlayer>();
        if (modPlayer == null)
            return;

        modPlayer.TargetKind = SpectatorTargetKind.Player;
        modPlayer.Target = playerIndex;
        modPlayer.TargetNPC = -1;
    }

    public static void SetNPCTarget(int npcIndex)
    {
        // UI helper: callers may queue a target before the local client receives mode sync.
        SpectatorPlayer modPlayer = LocalSpectatorPlayerOrNull();
        if (modPlayer == null && Main.netMode != NetmodeID.MultiplayerClient)
            return;

        modPlayer ??= LocalSpectatorPlayerOrNull() ?? LocalPlayerOrNull()?.GetModPlayer<SpectatorPlayer>();
        if (modPlayer == null)
            return;

        modPlayer.TargetKind = SpectatorTargetKind.NPC;
        modPlayer.TargetNPC = npcIndex;
        modPlayer.Target = -1;
    }

    public static void ToggleTargetSelection(SpectatorTargetKind preferredKind)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Player local = LocalSpectatorOrNull();
        if (local == null)
            return;

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        bool sameActiveTarget = preferredKind switch
        {
            SpectatorTargetKind.Player => modPlayer.TargetKind == SpectatorTargetKind.Player && GetPlayerTarget() != null,
            SpectatorTargetKind.NPC => modPlayer.TargetKind == SpectatorTargetKind.NPC && GetNPCTarget() != null,
            _ => false
        };

        if (sameActiveTarget)
        {
            modPlayer.ClearTarget();
            return;
        }

        if (preferredKind == SpectatorTargetKind.Player)
        {
            List<int> playerTargets = GetTargets(local.whoAmI);
            if (playerTargets.Count > 0)
                SetPlayerTarget(playerTargets[0]);
            else
            {
                modPlayer.ClearTarget();
                Main.NewText("No players found.", Color.OrangeRed);
            }

            return;
        }

        if (preferredKind == SpectatorTargetKind.NPC)
        {
            List<int> npcTargets = GetNPCTargets();
            if (npcTargets.Count > 0)
                SetNPCTarget(npcTargets[0]);
            else
            {
                modPlayer.ClearTarget();
                Main.NewText("No NPCs found.", Color.OrangeRed);
            }
        }
    }

    public static void EnsureTarget()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Player local = LocalPlayerOrNull();
        if (local == null)
            return;

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        if (!IsInSpectateMode(local))
        {
            modPlayer.ClearTarget();
            return;
        }

        if (modPlayer.TargetKind == SpectatorTargetKind.Player && GetPlayerTarget() != null)
            return;

        if (modPlayer.TargetKind == SpectatorTargetKind.NPC && GetNPCTarget() != null)
            return;

        modPlayer.ClearTarget();
    }

    public static void NextPlayerTarget()
    {
        Player local = LocalSpectatorOrNull();
        if (local == null)
            return;

        List<int> list = GetTargets(local.whoAmI);
        if (list.Count == 0)
        {
            local.GetModPlayer<SpectatorPlayer>().ClearTarget();
            return;
        }

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        SetPlayerTarget(list[NextIndex(list, modPlayer.Target, true)]);
    }

    public static void PreviousPlayerTarget()
    {
        Player local = LocalSpectatorOrNull();
        if (local == null)
            return;

        List<int> list = GetTargets(local.whoAmI);
        if (list.Count == 0)
        {
            local.GetModPlayer<SpectatorPlayer>().ClearTarget();
            return;
        }

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        SetPlayerTarget(list[NextIndex(list, modPlayer.Target, false)]);
    }

    public static void NextNPCTarget()
    {
        Player local = LocalSpectatorOrNull();
        if (local == null)
            return;

        List<int> list = GetNPCTargets();
        if (list.Count == 0)
        {
            local.GetModPlayer<SpectatorPlayer>().ClearTarget();
            return;
        }

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        SetNPCTarget(list[NextIndex(list, modPlayer.TargetNPC, true)]);
    }

    public static void PreviousNPCTarget()
    {
        Player local = LocalSpectatorOrNull();
        if (local == null)
            return;

        List<int> list = GetNPCTargets();
        if (list.Count == 0)
        {
            local.GetModPlayer<SpectatorPlayer>().ClearTarget();
            return;
        }

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        SetNPCTarget(list[NextIndex(list, modPlayer.TargetNPC, false)]);
    }

    public static Player GetPlayerTarget()
    {
        if (Main.netMode == NetmodeID.Server)
            return null;

        Player local = LocalSpectatorOrNull();
        if (local == null)
            return null;

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        int target = modPlayer.Target;
        if (modPlayer.TargetKind != SpectatorTargetKind.Player || target < 0 || target >= Main.maxPlayers)
            return null;

        Player player = Main.player[target];
        return player.active && IsInPlayerMode(player) && !player.ghost ? player : null;
    }

    public static NPC GetNPCTarget()
    {
        if (Main.netMode == NetmodeID.Server)
            return null;

        Player local = LocalSpectatorOrNull();
        if (local == null)
            return null;

        SpectatorPlayer modPlayer = local.GetModPlayer<SpectatorPlayer>();
        int target = modPlayer.TargetNPC;
        if (modPlayer.TargetKind != SpectatorTargetKind.NPC || target < 0 || target >= Main.maxNPCs)
            return null;

        NPC npc = Main.npc[target];
        return npc?.active == true ? npc : null;
    }

    public static Player GetTarget() => GetPlayerTarget();

    public static SpectatorTargetKind GetCurrentTargetKind()
    {
        SpectatorPlayer modPlayer = LocalSpectatorPlayerOrNull();
        return modPlayer?.TargetKind ?? SpectatorTargetKind.None;
    }

    public static string GetCurrentTargetText()
    {
        return GetCurrentTargetKind() switch
        {
            SpectatorTargetKind.Player => GetPlayerTarget() is Player player ? $"Spectating: {player.name}" : "Spectating: -",
            SpectatorTargetKind.NPC => GetNPCTarget() is NPC npc ? $"Spectating: {npc.FullName}" : "Spectating: -",
            _ => "Spectating: -"
        };
    }

    public static string GetCurrentTargetText(SpectatorTargetKind targetKind)
    {
        if (targetKind == SpectatorTargetKind.Player)
            return GetCurrentTargetKind() == SpectatorTargetKind.Player && GetPlayerTarget() is Player player ? $"Spectating: {player.name}" : "Spectate any player";

        if (targetKind == SpectatorTargetKind.NPC)
            return GetCurrentTargetKind() == SpectatorTargetKind.NPC && GetNPCTarget() is NPC npc ? $"Spectating: {npc.FullName}" : "Spectate any NPC";

        return "Spectating: -";
    }

    public static string GetTargetPanelTooltip(SpectatorTargetKind targetKind)
    {
        if (targetKind == SpectatorTargetKind.Player)
            return GetCurrentTargetKind() == SpectatorTargetKind.Player && GetPlayerTarget() != null ? "Click to unspectate" : "Click to spectate any player";

        if (targetKind == SpectatorTargetKind.NPC)
            return GetCurrentTargetKind() == SpectatorTargetKind.NPC && GetNPCTarget() != null ? "Click to unspectate" : "Click to spectate any NPC";

        return string.Empty;
    }

    public override void OnWorldLoad() => Modes.Clear();

    public override void OnWorldUnload() => Modes.Clear();

    public override void PreUpdatePlayers()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (!Main.player[i].active)
                Modes.Remove(i);

        if (Main.netMode == NetmodeID.Server)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || Modes.ContainsKey(i))
                    continue;

                PlayerMode defaultMode = GetJoinDefaultMode();
                Modes[i] = defaultMode;
                SpectatorNetHandler.SendMode(i, defaultMode);
            }

            return;
        }

        if (!Main.gameMenu)
            EnsureTarget();
    }
}

public static class SpectatorBridge
{
    public static bool IsInSpectateMode(Player player) => SpectatorSystem.IsInSpectateMode(player);

    public static void RequestToggleMode(int slot)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (SpectatorSystem.GetMode(slot) == PlayerMode.Spectator)
            SpectatorSystem.RequestSetPlayerMode(slot);
        else
            SpectatorSystem.RequestSetSpectatorMode(slot);
    }
}

public sealed class SpectatorPlayer : ModPlayer
{
    public int Target = -1;
    public int TargetNPC = -1;
    internal SpectatorTargetKind TargetKind = SpectatorTargetKind.None;
    private bool initialModeMessageHandled;

    public override void Initialize()
    {
        ClearTarget();
        initialModeMessageHandled = false;
    }

    public override void OnEnterWorld()
    {
        ClearTarget();
        initialModeMessageHandled = false;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SpectatorSystem.RequestFullSync();
            return;
        }

        if (SpectatorSystem.GetMode(Player.whoAmI) == PlayerMode.Spectator)
            Main.NewText("You are now a spectator.", Color.Yellow);

        initialModeMessageHandled = true;
    }

    public override void ModifyScreenPosition()
    {
        base.ModifyScreenPosition();

        if (!SpectatorSystem.IsInSpectateMode(Player))
            return;

        if (TargetKind == SpectatorTargetKind.Player && Target >= 0 && Target < Main.maxPlayers)
        {
            Player targetPlayer = Main.player[Target];
            if (targetPlayer.active)
            {
                Main.screenPosition = targetPlayer.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            }
            else
            {
                ClearTarget();
            }
        }
        else if (TargetKind == SpectatorTargetKind.NPC && TargetNPC >= 0 && TargetNPC < Main.maxNPCs)
        {
            NPC targetNpc = Main.npc[TargetNPC];
            if (targetNpc != null && targetNpc.active)
            {
                Main.screenPosition = targetNpc.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            }
            else
            {
                ClearTarget();
            }
        }
    }

    public void ClearTarget()
    {
        Target = -1;
        TargetNPC = -1;
        TargetKind = SpectatorTargetKind.None;
    }

    internal void HandleInitialModeMessage(PlayerMode mode)
    {
        if (initialModeMessageHandled)
            return;

        if (mode == PlayerMode.Spectator)
            Main.NewText("You are now a spectator.", Color.Yellow);

        initialModeMessageHandled = true;
    }
}
