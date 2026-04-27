using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.Map;
using PvPAdventure.Common.Spectator.Net;
using PvPAdventure.Common.Spectator.UI;
using PvPAdventure.Common.SSC;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.SpectatorMode;

/// <summary>
/// Allows players to be in either player mode or spectator mode, where they can spectate other players and have a free camera.
/// </summary>
public enum PlayerMode : byte
{
    Player,
    Spectator
}

/// <summary>
/// Handles the spectator mode system, including setting the player to be a spectator or 
/// </summary>
[Autoload(Side = ModSide.Both)]
internal sealed class SpectatorModeSystem : ModSystem
{
    internal static readonly Dictionary<int, PlayerMode> Modes = [];

    public static PlayerMode GetMode(int slot) => Modes.TryGetValue(slot, out PlayerMode mode) ? mode : PlayerMode.Player;

    public static bool IsInSpectateMode(Player player) => player?.active == true && GetMode(player.whoAmI) == PlayerMode.Spectator;

    public static bool IsInPlayerMode(Player player) => player?.active == true && GetMode(player.whoAmI) == PlayerMode.Player;

    internal static PlayerMode GetJoinDefaultMode() => ModContent.GetInstance<SpectatorConfig>().ForceSpectating ? PlayerMode.Spectator : PlayerMode.Player;

    public static void RequestSetLocalMode(PlayerMode mode)
    {
        if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SpectatorModeNetHandler.SendRequestSetMode(Main.myPlayer, mode);
            return;
        }

        SetModeLocal(Main.myPlayer, mode);
    }

    public static void SetModeServer(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.Server || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        Modes[slot] = mode;
        SpectatorModeNetHandler.SendSyncModes();
    }

    public static void SetModeLocal(int playerId, PlayerMode mode)
    {
        if (playerId < 0 || playerId >= Main.maxPlayers)
            return;

        PlayerMode oldMode = GetMode(playerId);
        Modes[playerId] = mode;

        if (playerId == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            //if (oldMode != PlayerMode.Spectator && mode == PlayerMode.Spectator)
                //MapRevealHelper.RevealLocalMap();

            //if (oldMode == PlayerMode.Spectator && mode == PlayerMode.Player)
                //MapRevealHelper.ClearLocalMap();
        }

        if (playerId != Main.myPlayer || Main.netMode == NetmodeID.Server)
            return;

        if (SSCDelayJoinSystem.IsWaitingForSSCLoad && mode != PlayerMode.Spectator)
        {
            Main.LocalPlayer.ghost = true;
            Log.Debug($"Player {playerId} received mode {mode} while SSC is loading; keeping ghost=true");
        }
        else
        {
            Main.LocalPlayer.ghost = mode == PlayerMode.Spectator;
        }
        Log.Chat($"Player {playerId} received mode: {mode}, player ghost now set to: {Main.LocalPlayer.ghost}");

        if (playerId == Main.myPlayer && oldMode != mode && Main.netMode != NetmodeID.Server)
            SpectatorUISystem.OnLocalModeAccepted(mode);

        if (mode == PlayerMode.Spectator)
            Main.playerInventory = false;
        else
            SpectatorTargetSystem.ClearTarget();
    }

    public static bool EnsureServerModes()
    {
        if (Main.netMode != NetmodeID.Server)
            return false;

        bool changed = false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!Main.player[i].active)
            {
                changed |= Modes.Remove(i);
                continue;
            }

            if (Modes.ContainsKey(i))
                continue;

            Modes[i] = GetJoinDefaultMode();
            changed = true;
        }

        return changed;
    }

    public override void PreUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.Server)
        {
            if (EnsureServerModes())
                SpectatorModeNetHandler.SendSyncModes();

#if DEBUG
            if (Main.GameUpdateCount % (60*7) == 0)
                Log.Chat(GetDebugStatusText());
#endif

            return;
        }

        Player local = Main.LocalPlayer;

        if (local?.active == true && GetMode(local.whoAmI) == PlayerMode.Spectator && !local.ghost)
            SetModeLocal(local.whoAmI, PlayerMode.Player);
    }

    public override void OnWorldLoad() => Reset();

    public override void OnWorldUnload() => Reset();

    private static void Reset()
    {
        Modes.Clear();
    }

    #region Debug

    private static string GetDebugStatusText()
    {
        int playersOnline = 0;
        int spectators = 0;
        StringBuilder names = new();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!Main.player[i].active)
                continue;

            playersOnline++;

            if (GetMode(i) != PlayerMode.Spectator)
                continue;

            if (spectators > 0)
                names.Append(", ");

            names.Append(Main.player[i].name);
            spectators++;
        }

        return $"Players online: {playersOnline}, spectators: {spectators} ({names})";
    }

    #endregion
}