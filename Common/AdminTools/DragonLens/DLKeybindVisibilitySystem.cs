using DragonLens.Core.Systems;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
internal sealed class DLKeybindVisibilitySystem : ModSystem
{
    private const string DragonLensModName = "DragonLens";
    private const string PvPAdventureModName = "PvPAdventure";

    private static readonly HashSet<string> PvPAdventureDragonLensKeybinds =
    [
        "DLEndGameTool",
        "DLEndGameToolRightClick",
        "DLPauseTool",
        "DLPointsSetterTool",
        "DLStartGameTool",
        "DLStartGameToolRightClick",
    ];

    private static readonly MethodInfo OnAssembleBindPanelsMethod =
        typeof(UIManageControls).GetMethod("OnAssembleBindPanels", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo KeybindsGetterMethod =
        typeof(KeybindLoader).GetProperty(nameof(KeybindLoader.Keybinds), BindingFlags.Static | BindingFlags.Public)?.GetMethod;

    private static readonly MethodInfo DragonLensVisibleKeybindsGetterMethod =
        typeof(PermissionHandler).Assembly
            .GetType("DragonLens.Core.Systems.DragonLensKeybindVisibilitySystem")
            ?.GetMethod("GetVisibleKeybinds", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

    private static readonly MethodInfo VisibleKeybindsGetterMethod =
        typeof(DLKeybindVisibilitySystem).GetMethod(nameof(GetVisibleKeybinds), BindingFlags.Static | BindingFlags.NonPublic);

    private static ILHook keybindVisibilityHook;

    private bool? lastVisible;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        if (OnAssembleBindPanelsMethod is null || KeybindsGetterMethod is null || VisibleKeybindsGetterMethod is null)
        {
            Mod.Logger.Warn("Could not hook Controls keybind visibility; PvPAdventure DragonLens keybinds will remain visible.");
            return;
        }

        keybindVisibilityHook = new ILHook(OnAssembleBindPanelsMethod, FilterPvPAdventureDragonLensKeybinds);
    }

    public override void Unload()
    {
        lastVisible = null;
        keybindVisibilityHook?.Dispose();
        keybindVisibilityHook = null;
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        bool visible = ShouldShowDragonLensKeybinds();

        if (lastVisible is null)
        {
            lastVisible = visible;
            return;
        }

        if (lastVisible == visible)
            return;

        lastVisible = visible;

        if (Main.InGameUI?.CurrentState == Main.ManageControlsMenu)
            Main.ManageControlsMenu.OnActivate();
    }

    private static void FilterPvPAdventureDragonLensKeybinds(ILContext il)
    {
        ILCursor cursor = new(il);

        if (TryReplaceKeybindSource(cursor, DragonLensVisibleKeybindsGetterMethod))
            return;

        if (TryReplaceKeybindSource(cursor, KeybindsGetterMethod))
            return;

        throw new InvalidOperationException("Could not find keybind source in UIManageControls.OnAssembleBindPanels.");
    }

    private static bool TryReplaceKeybindSource(ILCursor cursor, MethodInfo sourceMethod)
    {
        if (sourceMethod is null)
            return false;

        cursor.Index = 0;

        if (!cursor.TryGotoNext(instruction => instruction.MatchCall(sourceMethod)))
            return false;

        cursor.Next.OpCode = OpCodes.Call;
        cursor.Next.Operand = VisibleKeybindsGetterMethod;
        return true;
    }

    private static IEnumerable<ModKeybind> GetVisibleKeybinds()
    {
        IEnumerable<ModKeybind> keybinds = KeybindLoader.Keybinds;

        if (ShouldShowDragonLensKeybinds())
            return keybinds;

        return keybinds.Where(keybind => !IsHiddenAdminKeybind(keybind));
    }

    private static bool IsHiddenAdminKeybind(ModKeybind keybind)
    {
        if (keybind.Mod?.Name == DragonLensModName)
            return true;

        return keybind.Mod?.Name == PvPAdventureModName && PvPAdventureDragonLensKeybinds.Contains(keybind.Name);
    }

    private static bool ShouldShowDragonLensKeybinds()
    {
        return Main.LocalPlayer is not null && PermissionHandler.LooksLikeAdmin(Main.LocalPlayer);
    }
}
