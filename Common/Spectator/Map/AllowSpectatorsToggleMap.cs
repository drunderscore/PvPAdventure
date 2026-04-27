using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Map;

/// <summary>
/// Allows opening the fullscreen map as a ghost 
/// Also allows cycling the map styles as a ghost
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class AllowSpectatorsToggleMap : ModSystem
{
    public override void PostUpdateInput()
    {
        if (Main.dedServ || Main.LocalPlayer?.active != true)
            return;

        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
            return;

        if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput)
            return;

        // Close map when pressing escape
        if (Main.mapFullscreen && Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            Main.mapFullscreen = false;
            Main.mapStyle = 0;
            return;
        }

        bool mapDown = PlayerInput.Triggers.Current.MapFull;

        if (mapDown)
        {
            if (Main.LocalPlayer.releaseMapFullscreen)
                ToggleFullscreenMap();

            Main.LocalPlayer.releaseMapFullscreen = false;
        }
        else
        {
            Main.LocalPlayer.releaseMapFullscreen = true;
        }

        if (Main.mapFullscreen)
            UpdateFullscreenMapZoom();

        if (SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) && PlayerInput.Triggers.Current.MapStyle && !PlayerInput.Triggers.Old.MapStyle)
            CycleMapStyle();
    }

    private static void ToggleFullscreenMap()
    {
        if (!Main.mapFullscreen)
        {
            Main.LocalPlayer.TryOpeningFullscreenMap();
            Main.mapStyle = 1;
            return;
        }

        Main.mapFullscreen = false;
        Main.mapStyle = 0;
    }

    private static void UpdateFullscreenMapZoom()
    {
        float delta = PlayerInput.ScrollWheelDelta / 120f;

        if (PlayerInput.UsingGamepad)
            delta += (PlayerInput.Triggers.Current.HotbarPlus.ToInt() - PlayerInput.Triggers.Current.HotbarMinus.ToInt()) * 0.1f;

        if (delta == 0f)
            return;

        Main.mapFullscreenScale = MathHelper.Clamp(Main.mapFullscreenScale * (1f + delta * 0.3f), 0.1f, 31f);
    }

    private static void CycleMapStyle()
    {

        if (Main.mapFullscreen)
        {
            Main.mapFullscreen = false;
            Main.mapStyle = 1;
            return;
        }

        Main.mapStyle++;

        if (Main.mapStyle > 2)
            Main.mapStyle = 0;
    }
}