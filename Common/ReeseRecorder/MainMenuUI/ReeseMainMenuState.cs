using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.ReeseRecorder.MainMenuUI;

internal sealed class ReeseMainMenuState : UIState
{
    // Fields
    private UIList list;
    private static string GetReplaysFileDirectory() => Path.Combine(Main.SavePath, "PvPAdventureReplays");

    public override void OnInitialize()
    {
        // Dimensions
        const float widthPx = 320f;
        const float heightPx = 420f;
        const float marginPx = 20f;
        const float headerHeightPx = 28f;
        const float scrollbarWidthPx = 20f;

        // Root
        var root = new UIElement();
        root.Width.Set(widthPx, 0f);
        root.Height.Set(heightPx, 0f);
        root.Left.Set(-(widthPx + marginPx), 1f);
        root.Top.Set(marginPx, 0f);
        Append(root);

        // Header
        var header = new UIText("Reese", 1.15f, false);
        header.Width.Set(0f, 1f);
        header.Height.Set(headerHeightPx, 0f);
        root.Append(header);

        // Panel
        var panel = new UIPanel();
        panel.Width.Set(0f, 1f);
        panel.Height.Set(-headerHeightPx, 1f);
        panel.Top.Set(headerHeightPx, 0f);
        root.Append(panel);

        // List
        list = new UIList();
        list.Width.Set(-scrollbarWidthPx - 6f, 1f);
        list.Height.Set(-8f, 1f);
        list.Left.Set(4f, 0f);
        list.Top.Set(4f, 0f);
        list.ListPadding = 2f;
        panel.Append(list);

        // Scrollbar
        var scrollbar = new UIScrollbar();
        scrollbar.Width.Set(scrollbarWidthPx, 0f);
        scrollbar.Height.Set(-8f, 1f);
        scrollbar.Left.Set(-scrollbarWidthPx - 4f, 1f);
        scrollbar.Top.Set(4f, 0f);
        panel.Append(scrollbar);

        list.SetScrollbar(scrollbar);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        Rebuild();
    }

    private void Rebuild()
    {
        // Clear
        list.Clear();

        // Enumerate
        var dir = GetReplaysFileDirectory();
        Directory.CreateDirectory(dir);

        var entries = Directory.GetFileSystemEntries(dir);
        Array.Sort(entries, StringComparer.OrdinalIgnoreCase);

        // Always log
        LogReplays(entries);

        // Empty
        if (entries.Length == 0)
        {
            var noReplaysText = new UIText("No replays found. Click here to open folder.", 0.7f, false);
            noReplaysText.OnLeftClick += (_, _) =>
            {
                Log.Debug($"Open folder click: {dir}");
                try { Utils.OpenFolder(dir); } catch { }
            };
            list.Add(noReplaysText);
            return;
        }

        // Rows
        foreach (var path in entries)
        {
            var isDir = Directory.Exists(path);
            var isReplay = !isDir && path.EndsWith(".reese", StringComparison.OrdinalIgnoreCase);
            //if (!isDir && !isReplay)
                //continue;

            var name = Path.GetFileName(path) + (isDir ? "/" : "");
            AddRow(name, path, clickable: true);
        }
    }

    private void AddRow(string label, string fullPath, bool clickable)
    {
        // Colors
        var mainMenuWhite = new Color(237, 246, 255);
        var mainMenuGrey = new Color(173, 173, 198);

        // Create row
        var row = new UIPanel();
        row.Width.Set(0f, 1f);
        row.Height.Set(26f, 0f);
        row.PaddingTop = 4f;
        row.PaddingBottom = 4f;
        row.PaddingLeft = 6f;

        // Create text
        var text = new UIText(label, 0.7f, false)
        {
            VAlign = 0.5f,
            HAlign = 0f,
            TextColor = mainMenuGrey
        };
        row.Append(text);

        // Hover
        row.OnMouseOver += (_, _) => text.TextColor = mainMenuWhite;
        row.OnMouseOut += (_, _) => text.TextColor = mainMenuGrey;

        // Click
        if (clickable)
        {
            row.OnLeftClick += (_, _) =>
            {
                Log.Debug($"[ReeseMainMenuState] Click: {label} path={fullPath}");
                if (Directory.Exists(fullPath))
                {
                    try { Utils.OpenFolder(fullPath); } catch { }
                    return;
                }

                EnterReplay(fullPath);
            };
        }

        list.Add(row);
    }

    public static void EnterReplay(string demoPath)
    {
        Main.QueueMainThreadAction(() =>
        {
            // Select player
            Main.LoadPlayers();
            var player = Main.PlayerList.FirstOrDefault();
            if (player == null)
            {
                Main.menuMode = 0;
                return;
            }

            Main.SelectPlayer(player);
            Log.Debug($"Successfully selected {player.Player.name} for replay");

            if (!File.Exists(demoPath))
            {
                Log.Error("Error: No file demo found at: " + demoPath);
                Main.menuMode = 0;
                return;
            }

            // Debug replay size
            var replayMegaBytes = new FileInfo(demoPath).Length / (1024 * 1024);
            Log.Debug("Successfully found replay file, size: " + replayMegaBytes + " MB");

            // Enter magic IP, which will trigger ReeseRecorder to load the replay
            try
            {
                // Join the magic IP (code taken from Main.instance.OnSubmitServerPassword())
                Netplay.SetRemoteIP("10.2.3.4");
                Main.autoPass = true;
                Main.statusText = Lang.menu[8].Value;
                Netplay.StartTcpClient();
                Main.menuMode = 10;
            }
            catch
            {
                Log.Error("Failed to join magic IP thingy");
                //Main.menuMode = 0;
                Main.statusText = "Failed to join magic IP thingy";
            }
        });
    }

    private void LogReplays(string[] entries)
    {
        Log.Debug($"Found {entries.Length} entries in PvPAdventureReplays folder");

        for (var i = 0; i < entries.Length; i++)
        {
            var path = entries[i];
            var isDir = Directory.Exists(path);
            var name = Path.GetFileName(path) + (isDir ? "/" : "");
            long bytes = 0;

            if (!isDir)
            {
                try { bytes = new FileInfo(path).Length; } catch { }
            }

            Log.Debug($"Entry {i + 1}: {name}, bytes: {bytes}");
        }
    }
}