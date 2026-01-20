using DragonLens.Core.Loaders.UILoading;
using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Compat.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public sealed class DLUnpausedUICompat : ModSystem
{
    private delegate void orig_UpdateUI(UILoader self, GameTime gameTime);

    private static FieldInfo drawDelay;

    // Ensure we only run the hover-only pass once per frame.
    private static uint lastHoverPassFrame;

    public override void PostSetupContent()
    {
        var mi = typeof(UILoader).GetMethod(nameof(UILoader.UpdateUI), BindingFlags.Instance | BindingFlags.Public);
        if (mi != null)
        {
            MonoModHooks.Add(mi, OnUpdateUI);
        }
    }

    private static void OnUpdateUI(orig_UpdateUI orig, UILoader self, GameTime gameTime)
    {
        if (Main.netMode != NetmodeID.SinglePlayer && !PermissionHandler.CanUseTools(Main.LocalPlayer))
            return;

        if (UILoader.SortedUserInterfaces is null)
            return;

        // Your existing behavior: full interaction update.
        foreach (UserInterface eachState in UILoader.SortedUserInterfaces)
        {
            if (eachState?.CurrentState is SmartUIState s && s.Visible)
            {
                eachState.Update(gameTime);

                if (eachState.LeftMouse.WasDown && eachState.LeftMouse.LastDown is not null && eachState.LeftMouse.LastDown is not UIState)
                    Main.mouseLeft = false;

                if (eachState.RightMouse.WasDown && eachState.RightMouse.LastDown is not null && eachState.RightMouse.LastDown is not UIState)
                    Main.mouseRight = false;
            }
        }

        UnblockBrowserDraw();
    }

    private static void UnblockBrowserDraw()
    {
        drawDelay ??= typeof(UILoader).Assembly
            .GetType("DragonLens.Content.GUI.BrowserButton")
            ?.GetField("drawDelayTimer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (drawDelay != null && (int)drawDelay.GetValue(null) > 0)
        {
            drawDelay.SetValue(null, 0);
        }
    }
}
