using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.ServerList;

public class ServerListUIState : MainMenuPageUIState
{
    private const float PanelPadding = 12f;
    private const float ScrollbarWidth = 20f;
    private const float HeaderHeight = 28f;

    private const float ButtonWidth = 275f;
    private const float ButtonSmallWidth = 80f;

    private const float ServerColumnWidth = ButtonWidth;
    private const float PlayersColumnWidth = ButtonSmallWidth;
    private const float StatusColumnWidth = ButtonSmallWidth;

    private const float ServerColumnLeft = 0f;
    private const float PlayersColumnLeft = ServerColumnLeft + ServerColumnWidth;
    private const float StatusColumnLeft = PlayersColumnLeft + PlayersColumnWidth;

    private const float HeaderContentWidth = ServerColumnWidth + PlayersColumnWidth + StatusColumnWidth;

    private ServerListUIContent content;

    private UIList serverList = null!;
    private UIScrollbar scrollbar = null!;

    private ServerRow? selectedRow;
    private string? selectedIP;
    private int selectedPort;

    private int warningPromptTimer;

    private SortColumn sortColumn = SortColumn.Server;
    private bool sortAscending = true;

    private enum SortColumn
    {
        Server,
        Players,
        Status
    }

    internal sealed class ServerRow : UIPanel
    {
        public ServerEntryContent Entry { get; set; }
        public List<UIText> Texts { get; } = [];
    }

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.ServerList";

    protected override void Populate(UIPanel panel)
    {
        base.Populate(panel);
        UIElement headerBar = new();
        headerBar.Width.Set(HeaderContentWidth, 0f);
        headerBar.Height.Set(32f, 0f);
        headerBar.Top.Set(6f, 0f);
        panel.Append(headerBar);

        UIImageButton AddHeader(Asset<Texture2D> asset, string text, float left, float width, SortColumn column)
        {
            UIImageButton header = new(asset)
            {
                Left = { Pixels = left },
                Top = { Pixels = 0f },
                Width = { Pixels = width },
                Height = { Pixels = HeaderHeight }
            };
            header.SetHoverImage(asset == Ass.Button ? Ass.Button_Border : Ass.Button_Small_Border);
            header.SetVisibility(1f, 1f);
            headerBar.Append(header);

            UIText label = new(text, 1f);
            label.HAlign = 0.5f;
            label.VAlign = 0.48f;
            header.Append(label);

            header.OnLeftClick += (_, _) => ToggleSort(column);
            header.OnUpdate += _ =>
            {
                if (!header.IsMouseHovering)
                    return;

                string dir = sortColumn == column ? sortAscending ? "Ascending" : "Descending" : "Not active";
                Main.instance.MouseText($"Sort by {text} ({dir})");
            };

            return header;
        }

        AddHeader(Ass.Button, "Server", ServerColumnLeft, ServerColumnWidth, SortColumn.Server);
        AddHeader(Ass.Button_Small, "Players", PlayersColumnLeft, PlayersColumnWidth, SortColumn.Players);
        AddHeader(Ass.Button_Small, "Status", StatusColumnLeft, StatusColumnWidth, SortColumn.Status);

        UIElement contentRoot = new();
        contentRoot.Width.Set(-(ScrollbarWidth + 2f), 1f);
        contentRoot.Height.Set(-(HeaderHeight + 10f), 1f);
        contentRoot.Top.Set(HeaderHeight + 8f, 0f);
        panel.Append(contentRoot);

        scrollbar = new UIScrollbar();
        scrollbar.HAlign = 1f;
        scrollbar.Top.Set(HeaderHeight + 8f, 0f);
        scrollbar.Width.Set(ScrollbarWidth, 0f);
        scrollbar.Height.Set(-(HeaderHeight + 10f), 1f);
        panel.Append(scrollbar);

        serverList = new UIList();
        serverList.Width.Set(0f, 1f);
        serverList.Height.Set(0f, 1f);
        serverList.ListPadding = 2f;
        serverList.ManualSortMethod += _ => { };
        serverList.SetScrollbar(scrollbar);
        contentRoot.Append(serverList);
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
        serverList.Clear();
        serverList.Add(MainMenuPageUIState.CreateWrappedMessageElement(message, 0.9f, 140f));
        serverList.Recalculate();
    }

    private void RefreshList()
    {
        serverList.Clear();

        IEnumerable<ServerEntryContent> sorted = sortColumn switch
        {
            SortColumn.Server => sortAscending
                ? content.Entries.OrderBy(x => x.IP).ThenBy(x => x.Port)
                : content.Entries.OrderByDescending(x => x.IP).ThenByDescending(x => x.Port),

            SortColumn.Players => sortAscending
                ? content.Entries.OrderBy(x => x.Players).ThenBy(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port)
                : content.Entries.OrderByDescending(x => x.Players).ThenByDescending(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port),

            SortColumn.Status => sortAscending
                ? content.Entries.OrderByDescending(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port)
                : content.Entries.OrderBy(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port),

            _ => content.Entries
        };

        bool selectionStillVisible = false;

        foreach (ServerEntryContent entry in sorted)
        {
            ServerRow row = new();
            row.Entry = entry;
            row.Width.Set(0f, 1f);
            row.Height.Set(30f, 0f);
            row.SetPadding(0f);

            row.Append(MakeColumn(row, ServerColumnLeft, ServerColumnWidth, $"{entry.IP}:{entry.Port}", true));
            row.Append(MakeColumn(row, PlayersColumnLeft, PlayersColumnWidth, $"{entry.Players}/{entry.MaxPlayers}", false));
            row.Append(MakeColumn(row, StatusColumnLeft, StatusColumnWidth, entry.Status ? "Online" : "Offline", false));

            bool isSelected = selectedIP == entry.IP && selectedPort == entry.Port;
            ApplyRowStyle(row, isSelected, false);

            if (isSelected)
            {
                selectedRow = row;
                selectionStillVisible = true;
            }

            row.OnMouseOver += (_, _) =>
            {
                if (selectedRow == row)
                    return;

                ApplyRowStyle(row, false, true);
            };

            row.OnMouseOut += (_, _) =>
            {
                if (selectedRow == row)
                    return;

                ApplyRowStyle(row, false, false);
            };

            row.OnLeftClick += (_, _) => SelectRow(row);
            row.OnLeftDoubleClick += (_, _) =>
            {
                SelectRow(row);
                JoinServer(row.Entry.IP, row.Entry.Port);
            };

            serverList.Add(row);
        }

        if (!selectionStillVisible)
            ClearSelection();
    }

    private static UIElement MakeColumn(ServerRow row, float leftPixels, float widthPixels, string text, bool leftAligned)
    {
        UIElement column = new();
        column.IgnoresMouseInteraction = true;
        column.Left.Set(leftPixels - PanelPadding, 0f);
        column.Width.Set(widthPixels, 0f);
        column.Height.Set(0f, 1f);

        UIText label = new(text)
        {
            TextColor = Color.White,
            VAlign = 0.5f,
            IgnoresMouseInteraction = true
        };

        if (leftAligned)
        {
            label.Left.Set(30f, 0f);
            label.HAlign = 0f;
            label.TextOriginX = 0f;
        }
        else
        {
            label.HAlign = 0.5f;
        }

        column.Append(label);
        row.Texts.Add(label);
        return column;
    }

    private void ApplyRowStyle(ServerRow row, bool isSelected, bool isHovered)
    {
        if (isSelected)
        {
            row.BackgroundColor = new Color(73, 94, 171) * 0.95f;
            row.BorderColor = new Color(89, 116, 213);
            return;
        }

        if (isHovered)
        {
            row.BackgroundColor = new Color(73, 94, 171) * 0.9f;
            row.BorderColor = new Color(89, 116, 213);
            return;
        }

        row.BackgroundColor = new Color(63, 82, 151) * 0.35f;
        row.BorderColor = new Color(89, 116, 213) * 0.25f;
    }

    private void SelectRow(ServerRow row)
    {
        if (selectedRow != null)
            ApplyRowStyle(selectedRow, false, false);

        selectedRow = row;
        selectedIP = row.Entry.IP;
        selectedPort = row.Entry.Port;

        ApplyRowStyle(row, true, false);
    }

    private void ClearSelection()
    {
        selectedRow = null;
        selectedIP = null;
        selectedPort = 0;
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);

        if (scrollbar != null)
            scrollbar.ViewPosition -= evt.ScrollWheelValue;
    }

    private void ToggleSort(SortColumn column)
    {
        if (sortColumn == column)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = column;
            sortAscending = true;
        }

        RefreshList();
    }

    private void JoinSelectedServer()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);

        if (selectedRow == null)
        {
            warningPromptTimer = 30;
            return;
        }

        JoinServer(selectedRow.Entry.IP, selectedRow.Entry.Port);
    }

    private void JoinServer(string host, int port)
    {
        Main.menuMultiplayer = true;
        Main.menuServer = false;
        Main.autoPass = true;

        Netplay.ListenPort = port;
        Main.getIP = host.Trim();

        Main.statusText = $"Resolving {host}:{port}...";

        Netplay.SetRemoteIPAsync(Main.getIP, () =>
        {
            Main.menuMode = 14;
            Main.statusText = $"Connecting to {host}:{port}";
            Netplay.StartTcpClient();
        });
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
