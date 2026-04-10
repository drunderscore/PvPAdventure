using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.State;

[Autoload(Side = ModSide.Client)]
public sealed class SpectatorUISystem : ModSystem
{
    private static UserInterface spectatorInterface;
    private static SpectatorUIState spectatorState;

    public static bool IsEnabled
    {
        get
        {
            var config = ModContent.GetInstance<SpectatorConfig>();
            if (config == null)
            {
                Log.Warn("SpectateConfig not loaded – Spectate disabled by default");
                return false;
            }

            return config.IsSpectateEnabled;
        }
    }

    public override void OnWorldLoad()
    {
        spectatorInterface = new();
        spectatorState = new();

        // If not debugging and in singleplayer, don't open the UI
#if !DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;
#endif

        // If config wants it to open, then open!
        var config = ModContent.GetInstance<SpectatorConfig>();
        if (config.IsSpectateEnabled && !config.ForcePlayersToSpectateWhenJoining)
        {
            ToggleSpectateJoinUI();
        }
    }

    public static void EnterPlayerMode()
    {
        SpectatorSystem.RequestSetLocalMode(PlayerMode.Player);
        CloseJoinUI();
        Main.LocalPlayer.ghost = false;
        Main.NewText("You are now a player.", Color.Yellow);
    }

    public static void EnterSpectateMode()
    {
        SpectatorSystem.RequestSetLocalMode(PlayerMode.Spectator);
        CloseJoinUI();
        Main.LocalPlayer.ghost = true;
        Main.playerInventory = false;
        Main.NewText("You are now a spectator.", Color.Yellow);
        Main.NewText("Use shift + wasd to move as a ghost.", Color.Yellow);
        Main.NewText("Spectate is available in the top right corner of your screen^^", Color.Yellow);
    }

    public static void TogglePlayerSpectatorControls()
    {
        spectatorState?.TogglePlayerSpectatorControls();
    }

    public static void ToggleNpcSpectatorControls()
    {
        spectatorState?.ToggleNpcSpectatorControls();
    }

    public static void ToggleSpectateJoinUI()
    {
        spectatorState?.ToggleJoinPanel();
    }

    public static void CloseJoinUI()
    {
        spectatorState?.CloseJoinPanel();
    }

    public static void EnsurePlayerSpectatorControlsOpen()
    {
        spectatorState?.EnsurePlayerSpectatorControlsOpen();
    }

    public static void EnsureNpcSpectatorControlsOpen()
    {
        spectatorState?.EnsureNpcSpectatorControlsOpen();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ShouldShowSpectateUI())
        {
            if (spectatorInterface?.CurrentState == null)
                spectatorInterface?.SetState(spectatorState);
        }
        else
        {
            if (spectatorInterface?.CurrentState != null)
                spectatorInterface?.SetState(null);
        }

        spectatorInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        if (spectatorInterface?.CurrentState != null)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                "PvPAdventure: Spectator UI",
                () =>
                {
                    spectatorInterface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }

    private static bool ShouldShowSpectateUI()
    {
        if (!IsEnabled || Main.gameMenu)
            return false;

#if !DEBUG
    if (Main.netMode == NetmodeID.SinglePlayer)
        return false;
#endif

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return false;

        return SpectatorSystem.IsInSpectateMode(local) || spectatorState?.IsJoinPanelOpen() == true;
    }
}
