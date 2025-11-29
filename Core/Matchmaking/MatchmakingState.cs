using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System;
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
    public override void OnInitialize()
    {
        // Dimensions
        int screenH = Main.minScreenH;
        const int bottomMargin = 20;
        const int topOffset = 250;
        const int footerHeight = 50;

        // Root
        var root = new UIElement();
        root.Width.Set(600f, 0f);
        root.Top.Set(topOffset, 0f);
        root.Height.Set(screenH - topOffset, 0f);
        root.HAlign = 0.5f;

        // Panel
        var panel = new UIPanel();
        panel.BackgroundColor = new Color(38, 66, 58) * 0.8f; // muted forest-teal tone
        panel.Width.Set(0f, 1f);
        panel.Height.Set(screenH - topOffset - footerHeight - bottomMargin * 2, 0f);
        panel.SetPadding(12);
        panel.OverflowHidden = false;
        root.Append(panel);

        // Title
        var title = new UITextPanel<string>("Matchmaking", 0.8f, true);
        title.HAlign = 0.5f;
        title.Top.Set(-46f, 0f);
        title.SetPadding(15f);
        title.BackgroundColor = new Color(86, 122, 78); // soft desaturated green
        root.Append(title);

        // Header bar
        var headerBar = new UIElement();
        headerBar.Width.Set(0, 1);
        headerBar.Height.Set(32f, 0);
        headerBar.Top.Set(0, 0f);
        panel.Append(headerBar);

        // Header
        void AddHeader(Asset<Texture2D> asset, string text, float xPos, float width)
        {
            var header = new UIImage(asset)
            {
                Left = { Pixels = xPos },
                Top = { Pixels = 0 },
                Width = { Pixels = width },
                Height = { Pixels = 32 }
            };
            panel.Append(header);

            var label = new UIText(text, 0.8f);
            label.HAlign = 0.5f;
            label.VAlign = 0.5f;
            label.TextOriginX = 0;
            label.TextOriginY = 0;
            label.Left.Set(6, 0);

            header.Append(label);
        }

        // Headers
        AddHeader(Ass.Button, "Server Name", 0, 200);
        AddHeader(Ass.Button, "IP and Port", 200, 200);
        AddHeader(Ass.Button_Small, "Players", 400, 60);
        AddHeader(Ass.Button_Small, "Ping", 460, 60);
        AddHeader(Ass.Button_Small, "Status", 520, 60);

        var contentRoot = new UIElement();
        contentRoot.Width.Set(0f, 1f);
        contentRoot.Height.Set(0f, 1f);
        contentRoot.Top.Set(30f, 0f); // below headers
        panel.Append(contentRoot);

        // UIList
        var contentList = new UIList();
        contentList.Width.Set(-20f, 1f);
        contentList.Height.Set(-30f, 1f);
        contentList.ListPadding = 6f;
        contentList.ManualSortMethod = _ => { };
        contentRoot.Append(contentList);

        // Scrollbar
        var scrollbar = new UIScrollbar();
        scrollbar.HAlign = 1f;
        scrollbar.Width.Set(20f, 0f);
        scrollbar.Height.Set(-32f, 1f);
        scrollbar.Left.Set(6, 0);
        scrollbar.Top.Set(6, 0);
        contentRoot.Append(scrollbar);

        contentList.SetScrollbar(scrollbar);

        void AddText(UIElement row, string text, float x)
        {
            var uiText = new UIText(text, 0.8f);
            uiText.Left.Set(x, 0f);
            uiText.VAlign = 0.5f;
            row.Append(uiText);
        }
        for (int i = 1; i < 30; i++)
        {
            var row = new UIElement();
            row.Width.Set(0f, 1f);
            row.Height.Set(32f, 0f);
            row.HAlign = 0.5f;

            AddText(row, $"Terraria PvP Adventure #{i}", 5);   // Name
            AddText(row, $"123.123.123.{i}:5555", 205);  // Server
            AddText(row, $"{Main.rand.Next(1, 8)}/8", 405); // Players
            AddText(row, $"{Main.rand.Next(20, 120)}", 465); // Ping
            AddText(row, (Main.rand.NextBool() ? "Up" : "-"), 525); // Status

            contentList.Add(row);
        }

        // Button
        UITextPanel<string> CreateButton(string text, Color idleBg, Color hoverBg)
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
        var backButton = CreateButton(Language.GetTextValue("UI.Back"),new Color(86, 122, 78) * 0.70f,new Color(156, 188, 132));
        backButton.HAlign = 0f;
        backButton.OnLeftClick += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.menuMode = 0;
        };
        root.Append(backButton);

        // Play button
        var playButton = CreateButton("Join",new Color(76, 175, 80),new Color(102, 187, 106));
        playButton.HAlign = 1f;
        playButton.OnLeftClick += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            // TODO: Implement matchmaking logic here
        };
        root.Append(playButton);

        Append(root);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Handle escape press
        if (Main.hasFocus && Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            Main.menuMode = 0;
        }
    }
}