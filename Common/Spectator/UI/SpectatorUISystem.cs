using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

[Autoload(Side = ModSide.Client)]
public class SpectatorUISystem : ModSystem
{
    private static UserInterface spectatorInterface;
    private static SpectatorUIState spectatorState;

    public override void OnWorldLoad()
    {
        spectatorInterface = new();
        spectatorState = new();
    }

    private static void EnsureInitialized()
    {
        spectatorInterface ??= new();
        spectatorState ??= new();
    }

    public static void ToggleSpectatePanel()
    {
        EnsureInitialized();

        bool wasOpen = spectatorState?.IsSpectatePanelOpen() == true;
        spectatorState?.ToggleSpectatePanel();
        bool isOpen = spectatorState?.IsSpectatePanelOpen() == true;

        PlayToggleSound(wasOpen, isOpen);
    }

    public static void OnLocalModeAccepted(PlayerMode mode)
    {
        if (mode == PlayerMode.Spectator)
        {
            Main.playerInventory = false;
            Main.NewText("You are now a spectator. Use free camera or select a player to spectate.", Color.Yellow);
            EnsurePlayerSpectatorControlsOpen();
            return;
        }

        Main.NewText("You are now a player.", Color.Yellow);
    }

    private static void PlayToggleSound(bool wasOpen, bool isOpen)
    {
        if (wasOpen == isOpen)
            return;

        SoundEngine.PlaySound(isOpen ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    public static void EnsurePlayerSpectatorControlsOpen()
    {
        spectatorState?.EnsurePlayerSpectatorControlsOpen();
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
        if (Main.gameMenu)
            return false;

        // Always Show the UI in debug mode for testing purposes.
//#if !DEBUG
//        if (Main.netMode == NetmodeID.SinglePlayer)
//            return false;
//#endif

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return false;

        return SpectatorModeSystem.IsInSpectateMode(local);
    }
}
