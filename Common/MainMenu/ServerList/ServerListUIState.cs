using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.ServerList;

public class ServerListUIState : MainMenuPageUIState
{
    private const float ButtonWidth = 275f;
    private const float ButtonSmallWidth = 80f;

    private ServerListUIContent content;

    private UISortableTable<ServerEntryContent> serverTable = null!;
    private string? selectedIP;
    private int selectedPort;

    private int warningPromptTimer;

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.ServerList";

    protected override void Populate(UIPanel panel)
    {
        base.Populate(panel);
        serverTable = new UISortableTable<ServerEntryContent>(
            [
                new SortableTableColumn<ServerEntryContent>(
                    "Server",
                    Ass.Button,
                    Ass.Button_Border,
                    ButtonWidth,
                    entry => $"{entry.IP}:{entry.Port}",
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.IP).ThenBy(x => x.Port)
                        : items.OrderByDescending(x => x.IP).ThenByDescending(x => x.Port),
                    leftAligned: true,
                    textInset: 30f),
                new SortableTableColumn<ServerEntryContent>(
                    "Players",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => $"{entry.Players}/{entry.MaxPlayers}",
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Players).ThenBy(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port)
                        : items.OrderByDescending(x => x.Players).ThenByDescending(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port)),
                new SortableTableColumn<ServerEntryContent>(
                    "Status",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => entry.Status ? "Online" : "Offline",
                    (items, ascending) => ascending
                        ? items.OrderByDescending(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port)
                        : items.OrderBy(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port))
            ]);

        serverTable.IsSelected = entry => selectedIP == entry.IP && selectedPort == entry.Port;
        serverTable.OnRowClicked = SelectRow;
        serverTable.OnRowDoubleClicked = entry =>
        {
            SelectRow(entry);
            JoinServer(entry.IP, entry.Port);
        };

        panel.Append(serverTable);
    }

    protected override void RefreshContent()
    {
        SetCurrentAsyncState(AsyncProviderState.Loading);

        bool buildExampleContent = true;
        content = buildExampleContent
            ? ServerListExampleContent.Create()
            : new ServerListUIContent([]);

        if (!buildExampleContent)
        {
            // TODO: Call the server list API here and map the response into ServerListUIContent.
        }

        if (content.Entries.Length == 0)
        {
            ShowContentMessage(MainMenuPageUIState.FormatErrorMessage("server list", "No servers found."));
            SetCurrentAsyncState(AsyncProviderState.Aborted);
            return;
        }

        RefreshList();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void ShowContentMessage(string message)
    {
        serverTable.ClearRows();
        serverTable.List.Add(MainMenuPageUIState.CreateWrappedMessageElement(message, 0.9f, 140f));
        serverTable.List.Recalculate();
    }

    private void RefreshList()
    {
        serverTable.SetItems(content.Entries);

        bool selectionStillVisible = content.Entries.Any(entry => selectedIP == entry.IP && selectedPort == entry.Port);
        if (selectionStillVisible)
        {
            serverTable.RefreshRows();
            return;
        }

        ClearSelection();

        if (content.Entries.Length > 0)
            SelectRow(content.Entries[0]);
        else
            serverTable.RefreshRows();
    }

    private void SelectRow(ServerEntryContent entry)
    {
        selectedIP = entry.IP;
        selectedPort = entry.Port;

        serverTable.RefreshRows();
        Log.Debug($"Selected server={selectedIP}:{selectedPort}");
    }

    private void ClearSelection()
    {
        selectedIP = null;
        selectedPort = 0;
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);

        if (serverTable != null)
            serverTable.Scrollbar.ViewPosition -= evt.ScrollWheelValue;
    }

    private void JoinSelectedServer()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);

        if (string.IsNullOrWhiteSpace(selectedIP) || selectedPort <= 0)
        {
            warningPromptTimer = 30;
            return;
        }

        JoinServer(selectedIP, selectedPort);
    }

    private void JoinServer(string host, int port)
    {
        string resolvedHost = host?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedHost) || port <= 0)
        {
            Log.Debug($"Join aborted host={resolvedHost}:{port}");
            warningPromptTimer = 30;
            return;
        }

        if (!EnsureSelectedPlayer(out PlayerFileData playerData))
        {
            warningPromptTimer = 30;
            return;
        }

        Log.Debug($"Joining server {resolvedHost}:{port} with {playerData.Player.name}");

        Main.menuMultiplayer = true;
        Main.menuServer = false;
        Main.autoPass = true;

        Netplay.ListenPort = port;
        Main.getIP = resolvedHost;

        Main.statusText = $"Resolving {resolvedHost}:{port}...";

        Netplay.SetRemoteIPAsync(Main.getIP, () =>
        {
            Log.Debug($"Resolved ip={Main.getIP}");
            Main.menuMode = 14;
            Main.statusText = $"Connecting to {resolvedHost}:{port}";
            Netplay.StartTcpClient();
        });
    }

    private static bool EnsureSelectedPlayer(out PlayerFileData playerData)
    {
        Main.LoadPlayers();

        List<PlayerFileData> players = Main.PlayerList?
            .Where(p => p?.Player != null && p.Player.loadStatus == 0)
            .ToList()
            ?? [];

        Log.Debug($"Loaded {players.Count} players");

        playerData = null;
        if (players.Count == 0)
            return false;

        playerData = Main.ActivePlayerFileData;
        if (playerData == null || playerData.Player == null || playerData.Player.loadStatus != 0 || !players.Contains(playerData))
            playerData = players[0];

        Main.ServerSideCharacter = false;
        Main.myPlayer = 0;
        playerData.SetAsActive();

        Log.Debug($"Selected player={playerData.Player.name}");
        return true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (warningPromptTimer > 0)
        {
            warningPromptTimer--;
            Main.instance.MouseText("Select a server first!");
        }
    }
}
