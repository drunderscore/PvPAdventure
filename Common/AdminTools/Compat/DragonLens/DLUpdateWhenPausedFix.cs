using System.Reflection;
using DragonLens.Core.Loaders.UILoading;
using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Compat.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public sealed class DLUnpausedUICompat : ModSystem
{
    private delegate void orig_UpdateUI(UILoader self, GameTime gameTime);

    static FieldInfo _drawDelay;

    public override void PostSetupContent()
    {
        var mi = typeof(UILoader).GetMethod(nameof(UILoader.UpdateUI),
            BindingFlags.Instance | BindingFlags.Public);

        if (mi != null)
            MonoModHooks.Add(mi, OnUpdateUI);
    }

    static void UnblockBrowserDraw()
    {
        _drawDelay ??= typeof(UILoader).Assembly
            .GetType("DragonLens.Content.GUI.BrowserButton")
            ?.GetField("drawDelayTimer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (_drawDelay != null && (int)_drawDelay.GetValue(null) > 0)
            _drawDelay.SetValue(null, 0);
    }

    private static void OnUpdateUI(orig_UpdateUI orig, UILoader self, GameTime gameTime)
    {
        if (!Main.gamePaused)
        {
            orig(self, gameTime);
            return;
        }

        if (UILoader.SortedUserInterfaces is null || Main.ingameOptionsWindow)
            return;

        // If any DL UI is visible while paused, ensure browser buttons are allowed to draw.
        bool anyVisible = false;
        foreach (var ui in UILoader.SortedUserInterfaces)
        {
            if (ui?.CurrentState is SmartUIState s && s.Visible) { anyVisible = true; break; }
        }
        if (!anyVisible)
        {
            orig(self, gameTime);
            return;
        }

        UnblockBrowserDraw();

        foreach (var eachState in UILoader.SortedUserInterfaces)
        {
            if (eachState?.CurrentState is SmartUIState s && s.Visible)
            {
                if (Main.netMode != NetmodeID.SinglePlayer && !PermissionHandler.CanUseTools(Main.LocalPlayer))
                    continue;

                eachState.Update(gameTime);

                // Optional but helps if dimensions were stale from earlier-paused frames.
                eachState.CurrentState?.Recalculate();

                if (eachState.LeftMouse.WasDown && eachState.LeftMouse.LastDown is not null && eachState.LeftMouse.LastDown is not UIState)
                    Main.mouseLeft = false;

                if (eachState.RightMouse.WasDown && eachState.RightMouse.LastDown is not null && eachState.RightMouse.LastDown is not UIState)
                    Main.mouseRight = false;
            }
        }
    }
}
