using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.PlayServerList;

public class PlayServerListUIState : ResizableUIState
{
    private const int BottomMargin = 20;
    private const int TopOffset = 220;
    private const int FooterHeight = 50;

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
    private const float RootWidth = HeaderContentWidth + ScrollbarWidth + PanelPadding * 2f + 8f;

    private readonly List<ServerEntry> servers = [];

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

    internal sealed class ServerEntry
    {
        public string IP { get; set; } = "";
        public int Port { get; set; }
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool Status { get; set; }
    }

    internal sealed class ServerRow : UIPanel
    {
        public ServerEntry Entry { get; set; } = null!;
        public List<UIText> Texts { get; } = [];
    }

    public override void OnActivate()
    {
        base.OnActivate();
        RemoveAllChildren();

        int screenH = Main.minScreenH;

        UIElement root = new();
        root.Width.Set(RootWidth, 0f);
        root.Top.Set(TopOffset, 0f);
        root.Height.Set(screenH - TopOffset, 0f);
        root.HAlign = 0.5f;

        UIPanel panel = new();
        panel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
        panel.BorderColor = Color.Black;
        panel.Width.Set(0f, 1f);
        panel.Height.Set(screenH - TopOffset - FooterHeight - BottomMargin * 2, 0f);
        panel.SetPadding(PanelPadding);
        root.Append(panel);

        UITextPanel<string> title = new("Play TPVPA!", 0.8f, true);
        title.HAlign = 0.5f;
        title.Top.Set(-46f, 0f);
        title.SetPadding(15f);
        title.BackgroundColor = new Color(73, 94, 171);
        root.Append(title);

        UIElement headerBar = new();
        headerBar.Width.Set(HeaderContentWidth, 0f);
        headerBar.Height.Set(32f, 0f);
        headerBar.Top.Set(6f, 0f);
        panel.Append(headerBar);

        UIImageButton AddHeader(Asset<Texture2D> asset, string text, float left, float width, SortColumn column)
        {
            var header = new UIImageButton(asset)
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

        var backButton = CreateNavigationButton("Back", new Color(63, 82, 151) * 0.8f, new Color(73, 94, 171));
        backButton.Width.Set(-10f, 0.5f);
        backButton.HAlign = 0f;
        backButton.OnLeftClick += (_, _) => GoBackToTPVPABrowserState();
        root.Append(backButton);

        var joinButton = CreateNavigationButton("Join", new Color(5, 140, 8), new Color(10, 145, 15));
        joinButton.Width.Set(-10f, 0.5f);
        joinButton.HAlign = 1f;
        joinButton.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);

            if (selectedIP == null)
            {
                warningPromptTimer = 30;
                return;
            }

            JoinServer(selectedIP, selectedPort);
        };
        root.Append(joinButton);

        Append(root);

        SeedExampleEntries();
        RefreshList();
    }

    private UITextPanel<string> CreateNavigationButton(string text, Color idleBg, Color hoverBg)
    {
        var button = new UITextPanel<string>(text, 0.7f, true);
        button.SetPadding(10f);
        button.Height.Set(50f, 0f);
        button.VAlign = 1f;
        button.Top.Set(-BottomMargin, 0f);

        Color idleBorder = Color.Black;
        Color hoverBorder = Colors.FancyUIFatButtonMouseOver;
        bool playedTick = false;

        button.BackgroundColor = idleBg;
        button.BorderColor = idleBorder;

        button.OnMouseOver += (_, _) =>
        {
            button.BackgroundColor = hoverBg;
            button.BorderColor = hoverBorder;

            if (playedTick)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            playedTick = true;
        };

        button.OnMouseOut += (_, _) =>
        {
            button.BackgroundColor = idleBg;
            button.BorderColor = idleBorder;
            playedTick = false;
        };

        return button;
    }

    private void SeedExampleEntries()
    {
        servers.Clear();

        servers.Add(new ServerEntry { IP = "127.0.0.1", Port = 5555, Players = 0, MaxPlayers = 16, Status = true });
        servers.Add(new ServerEntry { IP = "127.0.0.1", Port = 7777, Players = 5, MaxPlayers = 16, Status = true });
        servers.Add(new ServerEntry { IP = "127.0.0.2", Port = 5555, Players = 3, MaxPlayers = 16, Status = false });
        servers.Add(new ServerEntry { IP = "127.0.0.3", Port = 5555, Players = 11, MaxPlayers = 16, Status = true });
        servers.Add(new ServerEntry { IP = "eu.tpvpa.net", Port = 7777, Players = 16, MaxPlayers = 16, Status = true });
        servers.Add(new ServerEntry { IP = "dev.tpvpa.net", Port = 5555, Players = 1, MaxPlayers = 8, Status = false });
    }

    private void RefreshList()
    {
        serverList.Clear();

        IEnumerable<ServerEntry> sorted = sortColumn switch
        {
            SortColumn.Server => sortAscending
                ? servers.OrderBy(x => x.IP).ThenBy(x => x.Port)
                : servers.OrderByDescending(x => x.IP).ThenByDescending(x => x.Port),

            SortColumn.Players => sortAscending
                ? servers.OrderBy(x => x.Players).ThenBy(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port)
                : servers.OrderByDescending(x => x.Players).ThenByDescending(x => x.MaxPlayers).ThenBy(x => x.IP).ThenBy(x => x.Port),

            SortColumn.Status => sortAscending
                ? servers.OrderByDescending(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port)
                : servers.OrderBy(x => x.Status).ThenBy(x => x.IP).ThenBy(x => x.Port),

            _ => servers
        };

        bool selectionStillVisible = false;

        foreach (var entry in sorted)
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
        column.Left.Set(leftPixels - PanelPadding, 0f);
        column.Width.Set(widthPixels, 0f);
        column.Height.Set(0f, 1f);

        UIText label = new(text)
        {
            TextColor = Color.White,
            VAlign = 0.5f
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

    private void JoinServer(string host, int port)
    {
        Main.menuMode = 0;

        // DON'T SELECT A PLAYER, WE DO IT IN THE TPVPA CHARACTER SELECT MENUS!!:
        ///<see cref="TPVPACharacterListItem"/>

        //Main.LoadPlayers();
        //var player = Main.PlayerList.FirstOrDefault();
        //if (player != null)
        //    Main.SelectPlayer(player);

        Main.menuMultiplayer = true;
        Main.menuServer = false;
        Main.autoPass = true;

        Netplay.ListenPort = port;
        Main.getIP = host.Trim();

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