using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.HerosMod.TeamSelector;

internal class TeamSelectorElement : UIElement
{
    // Team colors: 0–5
    internal static readonly Color[] TeamColors =
    {
        new Color(0xC5, 0xC1, 0xD8),
        new Color(0xDA, 0x3B, 0x3B),
        new Color(0x3B, 0xDA, 0x55),
        new Color(0x3B, 0x95, 0xDA),
        new Color(0xDA, 0xB7, 0x3B),
        new Color(0xE0, 0x64, 0xF2),
    };

    private bool dragging;
    private Vector2 dragOffset;

    private UIPanel titlePanel;
    private UIPanel contentPanel;
    private UIPanel refreshPanel;
    public bool needsRebuild = false;
    private readonly List<int> players = new();
    private readonly int[] lastTeams = new int[Main.maxPlayers];
    private readonly bool[] lastActive = new bool[Main.maxPlayers];


    private Asset<Texture2D> pvpAsset;
    private Asset<Texture2D> pvpAssetHover;

    public override void OnInitialize()
    {
        Width.Set(350, 0);
        Height.Set(460, 0);
        Top.Set(0, 0);
        Left.Set(0, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;
        SetPadding(0);

        titlePanel = new();
        titlePanel.Height.Set(40, 0);
        titlePanel.Width.Set(0, 1);
        titlePanel.SetPadding(0);
        titlePanel.BackgroundColor = new Color(63, 82, 151) * 0.7f;

        UIText titleText = new("Assign Teams", large: true, textScale: 0.7f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        titlePanel.Append(titleText);

        contentPanel = new UIPanel
        {
            Top = new StyleDimension(40, 0),
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(-40, 1),
            BackgroundColor = new Color(20, 20, 60)*0.7f,
            BorderColor = Color.Black
        };
        contentPanel.SetPadding(0);
        Append(contentPanel);

        UIPanel closePanel = new()
        {
            Height = new StyleDimension(0, precent: 1),
            Width = new StyleDimension(pixels: 40, 0),
            HAlign = 1f,
            VAlign = 0.5f
        };
        closePanel.OnLeftClick += (_, _) => ModContent.GetInstance<TeamSelectorSystem>().ToggleActive();
        closePanel.OnMouseOver += (_, _) => closePanel.BorderColor = Color.Yellow;
        closePanel.OnMouseOut += (_, _) => closePanel.BorderColor = Color.Black;
        UIText closeText = new("X", large: true, textScale: 0.55f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        closePanel.Append(closeText);
        titlePanel.Append(closePanel);

        titlePanel.SetPadding(0);
        Append(titlePanel);

        // Refresh panel
        refreshPanel = new()
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            VAlign = 0.5f
        };
        refreshPanel.OnLeftClick += (_, _) =>
        {
            if (players != null)
            {
                players.Sort((a, b) => Main.player[a].team.CompareTo(Main.player[b].team));
                needsRebuild = true;
            }
        };
        refreshPanel.OnMouseOver += (_, _) =>
        {
            refreshPanel.BorderColor = Color.Yellow;
        };
        refreshPanel.OnMouseOut += (_, _) => refreshPanel.BorderColor = Color.Black;
        refreshPanel.SetPadding(0);
        refreshPanel.Append(new UIImage(Ass.Reset.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });
        titlePanel.Append(refreshPanel);

        // Assets
        pvpAsset = Main.Assets.Request<Texture2D>("Images/UI/PVP_1");
        pvpAssetHover = Main.Assets.Request<Texture2D>("Images/UI/PVP_2");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (refreshPanel.IsMouseHovering)
            Main.instance.MouseText("Refresh");

        UpdateDragging();

        // Check if we need to rebuild
        players.Clear();
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            var p = Main.player[i];

            bool wasActive = lastActive[i];
            bool isActive = p.active;

            if (isActive)
                players.Add(i);

            if (wasActive != isActive || lastTeams[i] != p.team)
            {
                lastActive[i] = isActive;
                lastTeams[i] = p.team;
                needsRebuild = true;
                break;
            }
        }

        // Rebuild if needed
        //if (needsRebuild)
        {
            needsRebuild = false;

            RemoveAllChildren();
            contentPanel.RemoveAllChildren();

            Append(titlePanel);
            Append(contentPanel);

            if (pvpAsset == null || pvpAssetHover == null)
                return;

            Texture2D sheetTex = pvpAsset.Value;

            int iconCount = 6;
            int iconWidth = sheetTex.Width / iconCount;
            int iconHeight = sheetTex.Height;

            int[] teamCounts = new int[iconCount];
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    int idx = players[i];
                    Player p = Main.player[idx];
                    int t = p.team;
                    if (t >= 0 && t < iconCount)
                        teamCounts[t]++;
                }
            }

            UIPanel summaryRow = new()
            {
                Width = new StyleDimension(0, 1),
                Height = new StyleDimension(24, 0),
                BackgroundColor = new Color(10, 10, 10) * 0.9f,
                BorderColor = Color.Black
            };
            summaryRow.SetPadding(4);

            string summaryText =
                $"None:{teamCounts[0]}  Red:{teamCounts[1]}  Green:{teamCounts[2]}  Blue:{teamCounts[3]}  Yellow:{teamCounts[4]}  Pink:{teamCounts[5]}";

            UIText summaryTextElement = new(summaryText, textScale: 0.8f)
            {
                VAlign = 0.5f,
                Left = new StyleDimension(10, 0)
            };
            summaryRow.Append(summaryTextElement);
            contentPanel.Append(summaryRow);

            var panelDims = GetDimensions();
            int spacing = 4;
            float totalIconsWidth = iconCount * iconWidth + (iconCount - 1) * spacing;
            int iconsLeft = (int)(panelDims.Width - totalIconsWidth - 10);

            int rowTopStart = 20;

            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    int playerIndex = players[i];
                    Player player = Main.player[playerIndex];

                    int team = player.team;
                    if (team < 0 || team >= TeamColors.Length)
                        team = 0;
                    Color rowColor = TeamColors[team];

                    UIPanel playerRow = new()
                    {
                        Top = new StyleDimension(rowTopStart + i * 30, 0),
                        Width = new StyleDimension(0, 1),
                        Height = new StyleDimension(30, 0),
                        BackgroundColor = rowColor,
                        BorderColor = Color.Black
                    };
                    playerRow.SetPadding(0);

                    string playerName = player == Main.LocalPlayer ? player.name + " (you)" : player.name;

                    UIText playerText = new(playerName, large: false, textScale: 1.0f)
                    {
                        Left = new StyleDimension(10, 0),
                        VAlign = 0.5f
                    };
                    playerRow.Append(playerText);

                    for (int teamIndex = 0; teamIndex < iconCount; teamIndex++)
                    {
                        int x = teamIndex * iconWidth;
                        Rectangle src = new(x, 0, iconWidth, iconHeight);

                        var icon = new TeamIconElement(pvpAsset, pvpAssetHover, src, player, teamIndex)
                        {
                            Left = new StyleDimension(iconsLeft + teamIndex * (iconWidth + spacing), 0),
                            VAlign = 0.5f
                        };

                        playerRow.Append(icon);
                    }

                    contentPanel.Append(playerRow);
                }
            }
        }
    }

    private void UpdateDragging()
    {
        if (dragging)
        {
            float x = Main.mouseX - dragOffset.X;
            float y = Main.mouseY - dragOffset.Y;

            var d = GetDimensions();
            float w = d.Width;
            float h = d.Height;

            //if (x < 0) x = 0;
            //else if (x + w > Main.screenWidth) x = Main.screenWidth - w;

            //if (y < 0) y = 0;
            //else if (y + h > Main.screenHeight) y = Main.screenHeight - h;

            Left.Pixels = x;
            Top.Pixels = y;
            Recalculate();
        }

        Rectangle parentSpace = Parent.GetDimensions().ToRectangle();
        if (!GetDimensions().ToRectangle().Intersects(parentSpace))
        {
            Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
            Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
            Recalculate();
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        if (titlePanel != null && titlePanel.ContainsPoint(evt.MousePosition))
        {
            dragging = true;
            dragOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        dragging = false;
        Recalculate();
    }
}

