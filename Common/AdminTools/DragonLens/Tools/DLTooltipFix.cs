using System.Reflection;
using DragonLens.Content.GUI;
using DragonLens.Core.Loaders.UILoading;
using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public sealed class DLTooltipConfigCompat : ModSystem
{
    private delegate void orig_TooltipReset(Tooltip tooltip, On_Main.orig_Update orig, Main self, GameTime gameTime);

    public override void PostSetupContent()
    {
        // Hook the private instance method Tooltip.Reset(On_Main.orig_Update, Main, GameTime)
        MethodInfo resetMethod = typeof(Tooltip).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
        if (resetMethod != null)
        {
            MonoModHooks.Add(resetMethod, OnTooltipReset);
        }
    }

    private static void OnTooltipReset(
        orig_TooltipReset orig,
        Tooltip tooltip,
        On_Main.orig_Update origUpdate,
        Main self,
        GameTime gameTime
    )
    {
        // Run DragonLens' original Reset first. This calls Main.Update and then clears tooltip text.
        orig(tooltip, origUpdate, self, gameTime);

        // Only intervene when the ModConfig UI is open.
        if (!ReferenceEquals(Main.InGameUI?.CurrentState, Interface.modConfig))
        {
            return;
        }

        // Admin gate (same policy as your other hook).
        if (Main.netMode != NetmodeID.SinglePlayer && !PermissionHandler.CanUseTools(Main.LocalPlayer))
        {
            return;
        }

        if (UILoader.SortedUserInterfaces is null)
        {
            return;
        }

        // Hover-only repopulation pass AFTER the clear point.
        // Prevent double-activations: no clicks, just hover/tooltip logic.
        bool oldLeft = Main.mouseLeft;
        bool oldRight = Main.mouseRight;

        Main.mouseLeft = false;
        Main.mouseRight = false;

        try
        {
            foreach (UserInterface eachState in UILoader.SortedUserInterfaces)
            {
                if (eachState?.CurrentState is SmartUIState s && s.Visible)
                {
                    eachState.Update(gameTime);
                }
            }
        }
        finally
        {
            Main.mouseLeft = oldLeft;
            Main.mouseRight = oldRight;
        }
    }
}
