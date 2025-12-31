using System;
using System.IO;
using System.Reflection;
using DragonLens.Content.GUI;
using DragonLens.Content.Themes.BoxProviders;
using DragonLens.Content.Themes.IconProviders;
using DragonLens.Content.Tools;
using DragonLens.Content.Tools.Developer;
using DragonLens.Content.Tools.Editors;
using DragonLens.Content.Tools.Gameplay;
using DragonLens.Content.Tools.Map;
using DragonLens.Content.Tools.Multiplayer;
using DragonLens.Content.Tools.Spawners;
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolbarSystem;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.Debug;
using PvPAdventure.Common.Integrations.DragonLens.Tools;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;

namespace PvPAdventure.Common.Integrations.DragonLens;

// References:
// Adding a layout to the layout browser with grid.Add():
// https://github.com/ScalarVector1/DragonLens/blob/master/Content/GUI/LayoutPresetBrowser.cs#L25
// Creating a layout file with ToolbarHandler.ExportToFile():
// https://github.com/ScalarVector1/DragonLens/blob/master/Content/GUI/FirstTimeLayoutPresetMenu.cs
// Creating custom layouts using toolbars and tools:
// https://github.com/ScalarVector1/DragonLens/blob/master/Core/Systems/FirstTimeSetupSystem.cs

/// Adds a new layout to DragonLens
[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLLayout : ModSystem
{
    private delegate void orig_PopulateGrid(LayoutPresetBrowser self, UIGrid grid);

    public override void PostSetupContent()
    {
        OnPopulateGridHook();
    }

    private void OnPopulateGridHook()
    {
        MethodInfo populateGridMethod = typeof(LayoutPresetBrowser).GetMethod("PopulateGrid");
        if (populateGridMethod == null)
        {
            Log.Error("PopulateGrid not found in class LayoutPresetBrowser");
            return;
        }
        MonoModHooks.Add(populateGridMethod, OnPopulateGrid);
    }

    // This method adds the actual layouts to layout browser
    private void OnPopulateGrid(orig_PopulateGrid orig, LayoutPresetBrowser self, UIGrid grid)
    {
        orig(self, grid);

        string layout1 = "PvP Adventure";
        RegisterPvPAdventureLayout(layout1);

        string layout2 = "PvP Adventure+";
        RegisterPvPAdventurePlusLayout(layout2);

        // Add the layouts to the grid of layout browser
        grid.Add(new LayoutPresetButton(self, layout1, GetLayoutPath(layout1), "Basic general-purpose tools for a standard PvP Adventure admin."));
        grid.Add(new LayoutPresetButton(self, layout2, GetLayoutPath(layout2), "Advanced all-purpose tools for a more seasoned PvP Adventure admin."));
    }

    private static string RegisterPvPAdventureLayout(string layoutName)
    {
        ToolbarHandler.BuildPreset(layoutName, n =>
        {
            // bottom heros toolbar
            n.Add(
                new Toolbar(new Vector2(0.5f, 1f), Orientation.Horizontal, AutomaticHideOption.Never)
                .AddTool<ItemSpawner>()
                .AddTool<NPCSpawner>()
                .AddTool<Time>()
                .AddTool<Weather>()
                .AddTool<SpawnTool>() // enemy spawn rate
                .AddTool<CustomizeTool>()
            );

            // left PvPAdventure toolbar
            n.Add(
                new Toolbar(new Vector2(0f, 0.6f), Orientation.Vertical, AutomaticHideOption.Never)
                .AddTool<DLStartGameTool>()
                .AddTool<DLEndGameTool>()
                .AddTool<DLPauseTool>()
                .AddTool<DLTeamAssignerTool>()
                .AddTool<DLPointsSetterTool>()
                .AddTool<DLAdminManagerTool>()
            );
        },
        ThemeHandler.GetBoxProvider<SimpleBoxes>(),
        ThemeHandler.GetIconProvider<DefaultIcons>());

        ExportLayout(layoutName);

        return layoutName;
    }

    private static string RegisterPvPAdventurePlusLayout(string layoutName)
    {
        ToolbarHandler.BuildPreset(layoutName, n =>
        {
            // bottom toolbar
            n.Add(
                new Toolbar(new Vector2(0.5f, 1f), Orientation.Horizontal, AutomaticHideOption.Never)

                // regular tools
                .AddTool<ItemSpawner>()
                .AddTool<NPCSpawner>()
                .AddTool<Time>()
                .AddTool<Weather>()
                .AddTool<SpawnTool>() // enemy spawn rate
                .AddTool<PlayerManager>()
                .AddTool<PlayerEditorTool>()
                .AddTool<CustomizeTool>()
            );

            // left PvPAdventure toolbar
            n.Add(
               new Toolbar(new Vector2(0f, 0.6f), Orientation.Vertical, AutomaticHideOption.Never)
               .AddTool<DLStartGameTool>()
               .AddTool<DLEndGameTool>()
               .AddTool<DLPauseTool>()
               .AddTool<DLTeamAssignerTool>()
               .AddTool<DLPointsSetterTool>()
               .AddTool<DLAdminManagerTool>()
           );

            // right toolbar
            n.Add(
                new Toolbar(new Vector2(1f, 0.2f), Orientation.Vertical, AutomaticHideOption.Never)

                // cheat tools
                .AddTool<Godmode>()
                .AddTool<InfiniteReach>()
                .AddTool<NoClip>()
                .AddTool<SystemEditorTool>()
                .AddTool<AssetManager>()
            );

            // left map toolbar
            n.Add(
                new Toolbar(new Vector2(0f, 0.6f), Orientation.Vertical, AutomaticHideOption.NoMapScreen)
                .AddTool<MapTeleport>()
                .AddTool<RevealMap>()
                .AddTool<HideMap>()
                .AddTool<CustomizeTool>()
            );
        },
        ThemeHandler.GetBoxProvider<SimpleBoxes>(),
        ThemeHandler.GetIconProvider<DefaultIcons>());

        ExportLayout(layoutName);

        return layoutName;
    }


    // Helper to get folder path
    private static string GetLayoutPath(string layoutName)
    {
        return Path.Join(Main.SavePath, "DragonLensLayouts", layoutName);
    }

    // Helper to export a file to DragonLens layout folder path
    private static void ExportLayout(string layoutName)
    {
        try
        {
            string layoutPath = Path.Combine(Main.SavePath, "DragonLensLayouts", layoutName);
            Directory.CreateDirectory(Path.GetDirectoryName(layoutPath));
            ToolbarHandler.ExportToFile(layoutPath);
            Log.Info($"Successfully exported layout: {layoutName}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to export layout {layoutName}: {ex.Message}");
        }
    }
}