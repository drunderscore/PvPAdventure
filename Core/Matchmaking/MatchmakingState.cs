using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Helpers;
using PvPAdventure.Core.Matchmaking.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.Matchmaking;

/// <summary>
/// A state that allows the player to participate in matchmaking.
/// </summary>
public class MatchmakingState : UIState
{
    internal sealed class ServerEntry
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool Status { get; set; }
    }
    internal sealed class ServerRow : UIPanel
    {
        public ServerEntry Entry;
        public List<UIText> Texts = new List<UIText>();
    }

    // Content
    List<ServerEntry> servers = [];
    private UIList _serverList;
    private UIScrollbar _scrollbar;
    private UIElement _contentRoot;

    // Selection
    private UIPanel _selectedRow;
    private string _selectedIP;
    private int _selectedPort;

    // Filtering
    private string currentFilter = "";
    private bool updateNeeded = false;
    private enum SortColumn
    {
        Server,
        Players,
        Status
    }

    private SortColumn _sortColumn = SortColumn.Server;
    private bool _sortAscending = true;

    // Popup text
    private int warningPromptTimer = 0;

    public override void OnInitialize()
    {
        // Dimensions
        int screenH = Main.minScreenH;
        const int bottomMargin = 20;
        const int topOffset = 250;
        const int footerHeight = 50;

        // Root
        UIElement root = new();
        root.Width.Set(626, 0f);
        root.Top.Set(topOffset, 0f);
        root.Height.Set(screenH - topOffset, 0f);
        root.HAlign = 0.5f;

        // Panel
        UIPanel panel = new();
        panel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
        panel.Width.Set(0f, 1f);
        panel.Height.Set(screenH - topOffset - footerHeight - bottomMargin * 2, 0f);
        panel.SetPadding(12);
        panel.OverflowHidden = false;
        root.Append(panel);

        // Title
        UITextPanel<string> title = new("Server Browser", 0.8f, true);
        title.HAlign = 0.5f;
        title.Top.Set(-46f, 0f);
        title.SetPadding(15f);
        title.BackgroundColor = new Color(73, 94, 171); 
        root.Append(title);

        // Header bar
        UIElement headerBar = new();
        headerBar.Width.Set(0, 1);
        headerBar.Height.Set(32f, 0);
        headerBar.Top.Set(6, 0f);
        panel.Append(headerBar);

        // Header
        UIImageButton AddHeader(Asset<Texture2D> asset, string text, float xPos, float width, SortColumn column)
        {
            var header = new UIImageButton(asset)
            {
                Left = { Pixels = xPos },
                Top = { Pixels = 0 },
                Width = { Pixels = width },
                Height = { Pixels = 28 }
            };
            header.SetHoverImage(asset == Ass.Button ? Ass.Button_Border : Ass.Button_Small_Border);
            header.SetVisibility(1f, 1f);
            panel.Append(header);

            UIText label = new(text, 1.0f);
            label.HAlign = 0.5f;
            label.VAlign = 0.48f;
            header.Append(label);

            // Click = toggle sort
            header.OnLeftClick += (_, __) => ToggleSort(column);

            // Hover = show sort direction
            header.OnUpdate += _ =>
            {
                if (header.IsMouseHovering)
                {
                    string dir;
                    if (_sortColumn == column)
                        dir = _sortAscending ? "Ascending" : "Descending";
                    else
                        dir = "Not active";

                    Main.instance.MouseText($"Sort by {text} ({dir})");
                }
            };
            return header;
        }

        // Headers
        AddHeader(Ass.Button, "Server", 0, 275, SortColumn.Server);
        AddHeader(Ass.Button_Small, "Players", 275, 80, SortColumn.Players);
        AddHeader(Ass.Button_Small, "Status", 275 + 80, 80, SortColumn.Status);

        // Searchbox
        Searchbox searchbox = new("Type to search");
        searchbox.Left.Set(3, 0);
        searchbox.HAlign = 1f;
        searchbox.Top.Set(0, 0);
        searchbox.Height.Set(28, 0);
        searchbox.Width.Set(155, 0);
        searchbox.OnTextChanged += () =>
        {
            currentFilter = searchbox.currentString;
            updateNeeded = true;
        };
        panel.Append(searchbox);

        // Clear searchbox
        UIImageButton clearSearchButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel"))
        {
            HAlign = 1f,
            VAlign = 0.5f,
            Left = new StyleDimension(-2f, 0f)
        };
        clearSearchButton.OnLeftClick += (_,_) => searchbox.SetText("");
        searchbox.Append(clearSearchButton);

        // Content root
        _contentRoot = new UIElement();
        _contentRoot.Width.Set(-22, 1f);
        _contentRoot.Height.Set(-28 - 6, 1f);
        _contentRoot.Top.Set(28 + 6, 0f); // below headers
        panel.Append(_contentRoot);

        // Debug content root
        UIPanel dbPanel = new();
        dbPanel.BackgroundColor = new Color(73, 94, 171);
        dbPanel.Width.Set(0, 1);
        dbPanel.Height.Set(0, 1);
        dbPanel.IgnoresMouseInteraction = true;
        _contentRoot.Append(dbPanel);

        // Scrollbar
        _scrollbar = new UIScrollbar();
        _scrollbar.HAlign = 1f;
        _scrollbar.Left.Set(3, 0);
        _scrollbar.Top.Set(28 + 12, 0);
        _scrollbar.Height.Set(-34 - 12, 1);
        panel.Append(_scrollbar);

        // UIList
        _serverList = new UIList();
        _serverList.Width.Set(0, 1);
        _serverList.Height.Set(0, 1);
        _serverList.SetScrollbar(_scrollbar);
        _serverList.ListPadding = 2f;
        _serverList.ManualSortMethod += (_) => { };

        // Servers
        for (int i = 0; i < 20; i++)
        {
            ServerEntry exampleServer = new()
            {
                IP = "127.0.0.1",
                Port = 5555,
                Players = 0,
                MaxPlayers = 16,
                Status = true,
            };
            servers.Add(exampleServer);
        }
        for (int i = 0; i < 20; i++)
        {
            ServerEntry exampleServer = new()
            {
                IP = "127.0.0.2",
                Port = 5555,
                Players = 3,
                MaxPlayers = 16,
                Status = false,
            };
            servers.Add(exampleServer);
        }

        _contentRoot.Append(_serverList);

        RefreshList();

        // Navigation Button
        UITextPanel<string> CreateNavigationButton(string text, Color idleBg, Color hoverBg)
        {
            var button = new UITextPanel<string>(text, 0.7f, true);
            button.SetPadding(10f);
            button.Width.Set(-10f, 0.5f);
            button.Height.Set(50f, 0f);
            button.VAlign = 1f;
            button.Top.Set(-bottomMargin, 0f);

            Color idleBorder = Color.Black;
            Color hoverBorder = Colors.FancyUIFatButtonMouseOver;
            bool playedTick = false;

            button.BackgroundColor = idleBg;
            button.BorderColor = idleBorder;

            button.OnMouseOver += (_, __) =>
            {
                button.BackgroundColor = hoverBg;
                button.BorderColor = hoverBorder;
                if (!playedTick)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    playedTick = true;
                }
            };

            button.OnMouseOut += (_, __) =>
            {
                button.BackgroundColor = idleBg;
                button.BorderColor = idleBorder;
                playedTick = false;
            };

            return button;
        }

        // Back button
        var backButton = CreateNavigationButton(Language.GetTextValue("UI.Back"), new Color(63, 82, 151) * 0.8f, new Color(73, 94, 171));
        backButton.HAlign = 0f;
        backButton.OnLeftClick += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.menuMode = 0;
        };
        root.Append(backButton);

        // Play button
        var playButton = CreateNavigationButton("Join", new Color(5,140,8), new Color(10, 145, 15));
        playButton.HAlign = 1f;
        playButton.OnLeftClick += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);

            // TODO: Implement matchmaking logic here
            // Temporary join
            //JoinServer("127.0.0.1", 5555); 

            if (_selectedIP == null)
            {
                warningPromptTimer = 30; // 2 seconds
            }
        };
        playButton.OnUpdate += (_) => {
            if (playButton.IsMouseHovering)
            {
            }
        };
        root.Append(playButton);

        Append(root);
    }

    private void RefreshList()
    {
        _serverList.Clear();

        IEnumerable<ServerEntry> filtered = string.IsNullOrEmpty(currentFilter)
            ? servers
            : servers.Where(s => $"{s.IP}:{s.Port}".Contains(currentFilter, StringComparison.OrdinalIgnoreCase));

        // Apply sorting
        filtered = _sortColumn switch
        {
            SortColumn.Server => (_sortAscending
                ? filtered.OrderBy(s => s.IP).ThenBy(s => s.Port)
                : filtered.OrderByDescending(s => s.IP).ThenByDescending(s => s.Port)),

            SortColumn.Players => (_sortAscending
                ? filtered.OrderBy(s => s.Players).ThenBy(s => s.MaxPlayers)
                : filtered.OrderByDescending(s => s.Players).ThenByDescending(s => s.MaxPlayers)),

            SortColumn.Status => (_sortAscending
                ? filtered.OrderByDescending(s => s.Status) // Online first
                : filtered.OrderBy(s => s.Status)),          // Offline first

            _ => filtered
        };

        foreach (var exampleServer in filtered)
        {
            // Row panel
            ServerRow serverPanel = new ServerRow { Entry = exampleServer };
            serverPanel.BackgroundColor = new Color(10, 5, 50) * 0.4f;
            serverPanel.Width.Set(0, 1);
            serverPanel.Height.Set(28, 0);

            // Data
            string serverStr = $"{exampleServer.IP}:{exampleServer.Port}";
            string playersStr = $"{exampleServer.Players}/{exampleServer.MaxPlayers}";
            string statusStr = exampleServer.Status ? "Online" : "Offline";

            // Column helper: creates a column container at given X+width
            UIElement MakeColumn(float leftPixels, float widthPixels, string text)
            {
                var col = new UIElement();
                col.Left.Set(leftPixels-12, 0f);
                col.Width.Set(widthPixels, 0f);
                col.Height.Set(0f, 1f); // full row height

                var t = new UIText(text)
                {
                    TextColor = Color.White,
                    HAlign = 0.5f,
                    VAlign = 0.5f
                };

                col.Append(t);
                serverPanel.Texts.Add(t);
                return col;
            }

            // Columns: 275 / 80 / 80
            serverPanel.Append(MakeColumn(0, 275, serverStr));
            serverPanel.Append(MakeColumn(275, 80, playersStr));
            serverPanel.Append(MakeColumn(275 + 80, 80, statusStr));

            // Hover / select behaviour
            serverPanel.OnMouseOver += (_, __) =>
            {
                if (serverPanel != _selectedRow)
                    serverPanel.BackgroundColor = new Color(73, 204, 10) * 0.7f;
            };

            serverPanel.OnMouseOut += (_, __) =>
            {
                if (serverPanel != _selectedRow)
                    serverPanel.BackgroundColor = new Color(10, 5, 50) * 0.4f;
            };

            serverPanel.OnLeftClick += (_, el) =>
            {
                SelectRow((ServerRow)el);
            };

            serverPanel.OnLeftDoubleClick += (_, el) =>
            {
                SelectRow((ServerRow)el);
                JoinServer(_selectedIP, _selectedPort);
            };

            _serverList.Add(serverPanel);
        }
    }

    private void SelectRow(ServerRow row)
    {
        if (_selectedRow != null)
        {
            _selectedRow.BackgroundColor = new Color(10, 5, 50) * 0.4f;
            foreach (var t in ((ServerRow)_selectedRow).Texts)
            {
                t.TextColor = Color.White;
            }
        }
        _selectedRow = row;
        row.BackgroundColor = Color.Green;
        foreach (var t in row.Texts)
        {
            t.TextColor = Color.White;
        }
        _selectedIP = row.Entry.IP;
        _selectedPort = row.Entry.Port;
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);
        if (_scrollbar != null)
            _scrollbar.ViewPosition -= evt.ScrollWheelValue;
    }

    private void ToggleSort(SortColumn column)
    {
        if (_sortColumn == column)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        RefreshList();
    }

    private void JoinServer(string host, int port)
    {
        Main.menuMode = 0; 

        Main.LoadPlayers();
        var player = Main.PlayerList.FirstOrDefault();
        if (player != null) Main.SelectPlayer(player);

        Main.menuMultiplayer = true;
        Main.menuServer = false;
        Main.autoPass = true;

        Netplay.ListenPort = port;
        Main.getIP = (host ?? "").Trim();

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

        // Warning timer
        if (warningPromptTimer > 0)
        {
            warningPromptTimer--;
            Main.instance.MouseText("select a server first!");
        }

        // Refresh if filter updated
        if (updateNeeded)
        {
            RefreshList();
            updateNeeded = false;
        }


        // Handle escape press
        if (Main.hasFocus && Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            Main.menuMode = 0;
        }
    }
}


