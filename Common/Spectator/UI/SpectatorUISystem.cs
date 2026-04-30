using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

[Autoload(Side = ModSide.Client)]
public class SpectatorUISystem : ModSystem
{
    private UserInterface spectatorInterface;
    private SpectatorUIState spectatorState;
    private UserInterface spectatorSettingsInterface;
    private SpectatorSettingsPanelUIState spectatorSettingsPanelState;
    private SpectatorSettingsEyeUIState spectatorSettingsEyeState;
    private bool spectatorSettingsExpanded = true;

    public override void OnWorldLoad()
    {
        spectatorInterface = new();
        spectatorState = new();
        spectatorSettingsInterface = new();
        spectatorSettingsPanelState = new();
        spectatorSettingsEyeState = new();
    }

    public void RebuildUI()
    {
        spectatorState?.RebuildSpectatorControlsPanel();
        spectatorSettingsPanelState?.Rebuild();
    }

    public void ToggleSpectatorSettingsPanel()
    {
        spectatorSettingsExpanded = !spectatorSettingsExpanded;
        RefreshSpectatorSettingsState();
    }

    public void OnLocalModeAccepted(PlayerMode mode)
    {
        if (mode == PlayerMode.Spectator)
        {
            Main.playerInventory = false;
            Main.NewText("You are now a spectator.", Color.Yellow);
            EnsureSpectatorHUDStaysOpen();
            return;
        }

        Main.NewText("You are now a player.", Color.Yellow);
    }

    public void EnsureSpectatorHUDStaysOpen()
    {
        spectatorState?.EnsureSpectatorHUDStaysOpen();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ShouldShowSpectateUI())
        {
            if (spectatorInterface?.CurrentState == null)
            {
                spectatorInterface?.SetState(spectatorState);
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }

            RefreshSpectatorSettingsState();
        }
        else
        {
            if (spectatorInterface?.CurrentState != null)
            {
                spectatorInterface?.SetState(null);
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            if (spectatorSettingsInterface?.CurrentState != null)
                spectatorSettingsInterface?.SetState(null);
        }

        spectatorInterface?.Update(gameTime);
        spectatorSettingsInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        // TESTME: Draw the UI below the config?
        // Update: Seems to work!
        if (IsAnyConfigUIOpen())
            index = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

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

        if (spectatorSettingsInterface?.CurrentState != null)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                "PvPAdventure: Spectator Settings UI",
                () =>
                {
                    spectatorSettingsInterface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }

    private void RefreshSpectatorSettingsState()
    {
        if (spectatorSettingsInterface is null)
            return;

        UIState desiredState = spectatorSettingsExpanded ? spectatorSettingsPanelState : spectatorSettingsEyeState;

        if (spectatorSettingsInterface.CurrentState != desiredState)
            spectatorSettingsInterface.SetState(desiredState);
    }

    private static bool ShouldShowSpectateUI()
    {
        if (Main.gameMenu)
            return false;

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return false;

        return SpectatorModeSystem.IsInSpectateMode(local);
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return Main.ingameOptionsWindow || s is UIModConfig or UIModConfigList;
    }
}
