using DragonLens.Content.GUI;
using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Helpers;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLToolbarHideForNonAdmins : ModSystem
{
    private delegate void orig_DrawToolbars(ToolbarStateHandler self, Vector2 arg1, float arg2);

    private delegate void orig_Refresh(ToolbarState self);
    public override void PostSetupContent()
    {
        HookDrawToolbars();
        HookRefresh(); 
    }

    private static bool CanShowDLToolsUI()
    {
        if (Main.netMode == NetmodeID.Server)
            return false;

        if (Main.gameMenu || Main.LocalPlayer is null)
            return false;

        // Admin rule
        return PermissionHandler.CanUseTools(Main.LocalPlayer);
    }

    private void HookDrawToolbars()
    {
        MethodInfo m = typeof(ToolbarStateHandler).GetMethod("DrawToolbars", BindingFlags.Instance | BindingFlags.NonPublic);

        if (m is null)
        {
            Log.Warn("DragonLens ToolbarStateHandler.DrawToolbars not found.");
            return;
        }

        MonoModHooks.Add(m, OnDrawToolbars);
    }

    private void OnDrawToolbars(orig_DrawToolbars orig, ToolbarStateHandler self, Vector2 arg1, float arg2)
    {
        // If not allowed, do nothing => no UI update, no draw, no hover/click capture
        if (!CanShowDLToolsUI())
            return;

        orig(self, arg1, arg2);
    }

    private void HookRefresh()
    {
        MethodInfo m = typeof(ToolbarState).GetMethod("Refresh",BindingFlags.Instance | BindingFlags.Public);

        if (m is null)
        {
            Log.Warn("DragonLens ToolbarState.Refresh not found.");
            return;
        }

        MonoModHooks.Add(m, OnRefresh);
    }

    private void OnRefresh(orig_Refresh orig, ToolbarState self)
    {
        if (!CanShowDLToolsUI())
        {
            // Ensure it stays empty for non-admins
            self.RemoveAllChildren();
            return;
        }

        orig(self);
    }
}
