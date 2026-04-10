using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Tools.ArenasTool;

internal sealed class ArenasAdminPanel : UIDraggablePanel
{
    private readonly UITextPanel<string> sendToMainWorldButton;
    private readonly UITextPanel<string> sendToArenasButton;

    private UIList playerList;
    private UIScrollbar playerScrollbar;

    private readonly bool[] lastActive = new bool[Main.maxPlayers];
    private readonly int[] lastWhoAmI = new int[Main.maxPlayers];
    private bool needsRebuild = true;

    protected override float MinResizeW => 320f;
    protected override float MinResizeH => 260f;

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<ArenasAdminSystem>().ToggleActive();
    }

    public ArenasAdminPanel()
        : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLArenasAdminTool.Title"))
    {
        Width.Set(560, 0);
        Height.Set(420, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(6f);

        const float buttonsTop = 8f;
        const float buttonHeight = 44f;
        const float buttonGap = 12f;

        // Button 1 (top-left)
        sendToMainWorldButton = new UITextPanel<string>("Send all to main world")
        {
            Width = { Percent = 0.5f, Pixels = -(buttonGap * 0.5f) },
            Height = { Pixels = buttonHeight },
            Top = { Pixels = buttonsTop },
            Left = { Percent = 0f, Pixels = 0f }
        };
        sendToMainWorldButton.OnMouseOver += (_, _) => sendToMainWorldButton.BorderColor = Color.Yellow;
        sendToMainWorldButton.OnMouseOut += (_, _) => sendToMainWorldButton.BorderColor = Color.Black;
        sendToMainWorldButton.OnLeftClick += (_, _) =>
        {
            ArenasAdminNetHandler.Request(ArenasAdminNetHandler.ArenasAdminPacketType.SendAllToMainWorld);
        };

        // Button 2 (top-right)
        sendToArenasButton = new UITextPanel<string>("Send all to arenas")
        {
            Width = { Percent = 0.5f, Pixels = -(buttonGap * 0.5f) },
            Height = { Pixels = buttonHeight },
            Top = { Pixels = buttonsTop },
            Left = { Percent = 0.5f, Pixels = (buttonGap * 0.5f) }
        };
        sendToArenasButton.OnMouseOver += (_, _) => sendToArenasButton.BorderColor = Color.Yellow;
        sendToArenasButton.OnMouseOut += (_, _) => sendToArenasButton.BorderColor = Color.Black;
        sendToArenasButton.OnLeftClick += (_, _) =>
        {
            ArenasAdminNetHandler.Request(ArenasAdminNetHandler.ArenasAdminPacketType.SendAllToArenas);
        };

        ContentPanel.Append(sendToMainWorldButton);
        ContentPanel.Append(sendToArenasButton);

        // Players header
        float headerTop = buttonsTop + buttonHeight + 14f;

        var header = new UIText("Players", textScale: 0.9f)
        {
            Top = { Pixels = headerTop },
            Left = { Pixels = 4f }
        };
        ContentPanel.Append(header);

        BuildPlayerListUI(topPixels: headerTop + 22f);

        needsRebuild = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        needsRebuild = true;

        bool changed = false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            bool isActive = p != null && p.active;

            if (lastActive[i] != isActive)
            {
                lastActive[i] = isActive;
                changed = true;
            }

            int who = isActive ? p.whoAmI : -1;
            if (lastWhoAmI[i] != who)
            {
                lastWhoAmI[i] = who;
                changed = true;
            }
        }

        if (changed)
            needsRebuild = true;

        if (needsRebuild)
        {
            needsRebuild = false;
            RebuildPlayerList();
        }
    }

    private void BuildPlayerListUI(float topPixels)
    {
        playerList = new UIList
        {
            Top = { Pixels = topPixels },
            Width = { Pixels = -24f, Percent = 1f },
            Height = { Pixels = -topPixels - 8f, Percent = 1f }
        };
        playerList.SetPadding(0f);
        playerList.ListPadding = 6f;

        playerScrollbar = new UIScrollbar
        {
            Top = { Pixels = topPixels },
            Height = { Pixels = -topPixels - 8f, Percent = 1f },
            Width = { Pixels = 20f },
            Left = { Pixels = -20f, Percent = 1f }
        };

        playerList.SetScrollbar(playerScrollbar);

        ContentPanel.Append(playerList);
        ContentPanel.Append(playerScrollbar);
    }

    private void RebuildPlayerList()
    {
        playerList.Clear();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            playerList.Add(CreatePlayerRow(p));
        }

        playerList.Recalculate();
    }

    private UIElement CreatePlayerRow(Player player)
    {
        var row = new UIPanel
        {
            Width = { Percent = 1f },
            Height = { Pixels = 40f },
            BackgroundColor = new Color(10, 10, 10) * 0.65f,
            BorderColor = Color.Black
        };
        row.SetPadding(0f);
        row.PaddingLeft = 6f;

        row.OnMouseOver += (_, _) => row.BorderColor = Color.Yellow;
        row.OnMouseOut += (_, _) => row.BorderColor = Color.Black;

        string name = player == Main.LocalPlayer ? player.name + " (you)" : player.name;

        var nameText = new UIText(name, textScale: 0.9f)
        {
            VAlign = 0.5f
        };
        row.Append(nameText);

        int playerIndex = player.whoAmI;

        const float iconSize = 40f;
        const float iconSpacing = 0f;
        const float rightPadding = 12f;


        var arenasIcon = new UIImage(Ass.Icon_Arenas.Value)
        {
            ImageScale = 0.7f,
            Width = { Pixels = iconSize },
            Height = { Pixels = iconSize },
            Left = { Pixels = -rightPadding - iconSize, Percent = 1f },
            VAlign = 0f,
            HAlign = 0f,
            Top = StyleDimension.FromPixels(-6)
        };

        arenasIcon.OnMouseOver += (_, _) =>
        {
            Main.instance.MouseText($"Send {Main.player[player.whoAmI].name} to arenas");
            arenasIcon.Width.Set(iconSize, 0f);
            arenasIcon.Height.Set(iconSize, 0f);
            arenasIcon.Left.Set(-rightPadding - iconSize, 1f);
            arenasIcon.Recalculate();
        };

        arenasIcon.OnMouseOut += (_, _) =>
        {
            arenasIcon.Width.Set(iconSize, 0f);
            arenasIcon.Height.Set(iconSize, 0f);
            arenasIcon.Left.Set(-rightPadding - iconSize, 1f);
            arenasIcon.Recalculate();
        };

        arenasIcon.OnLeftClick += (_, _) =>
        {
            Main.NewText($"Send {Main.player[playerIndex].name} to arenas");
        };

        row.Append(arenasIcon);

        var playIcon = new UIImage(Ass.Icon_StartGame.Value)
        {
            ImageScale = 0.9f,
            Width = { Pixels = iconSize },
            Height = { Pixels = iconSize },
            Left = { Pixels = -rightPadding - iconSize - iconSpacing - iconSize, Percent = 1f },
            VAlign = 0f,
            HAlign = 0f,
            Top = StyleDimension.FromPixels(2)
        };

        playIcon.OnMouseOver += (_, _) =>
        {
            Main.instance.MouseText($"Send {Main.player[player.whoAmI].name} to main world");
            playIcon.Width.Set(iconSize, 0f);
            playIcon.Height.Set(iconSize, 0f);
            playIcon.Left.Set(-rightPadding - iconSize - iconSpacing - iconSize, 1f);
            playIcon.Recalculate();
        };

        playIcon.OnMouseOut += (_, _) =>
        {
            playIcon.Width.Set(iconSize, 0f);
            playIcon.Height.Set(iconSize, 0f);
            playIcon.Left.Set(-rightPadding - iconSize - iconSpacing - iconSize, 1f);
            playIcon.Recalculate();
        };

        playIcon.OnLeftClick += (_, _) =>
        {
            Main.NewText($"Send {Main.player[playerIndex].name} to main world");
        };

        row.Append(playIcon);

        return row;
    }
}
