using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.TeamSelector;

internal class TeamSelectorPanel : UIPanel
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
    public bool needsRebuild = false;
    private List<int> players;

    private Asset<Texture2D> pvpAsset;
    private Asset<Texture2D> pvpAssetHover;

    public override void OnInitialize()
    {
        Width.Set(350, 0);
        Height.Set(460, 0);
        Left.Set(Main.screenWidth * 0.8f - Width.Pixels / 2f, 0f);
        Top.Set(Main.screenHeight * 0.8f - Height.Pixels / 2f, 0f);
        BackgroundColor = new Color(27, 29, 85);
        SetPadding(0);

        titlePanel = new();
        titlePanel.Height.Set(40, 0);
        titlePanel.Width.Set(0, 1);
        titlePanel.SetPadding(0);

        UIText titleText = new("Assign Teams", large: true, textScale: 0.7f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        titlePanel.Append(titleText);

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
        UIPanel refreshPanel = new()
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            Left = new StyleDimension(0, 0),
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
            //Main.instance.MouseText("Refresh");
            UICommon.TooltipMouseText("Refresh");
        };
        refreshPanel.OnMouseOut += (_, _) => refreshPanel.BorderColor = Color.Black;
        refreshPanel.SetPadding(0);
        refreshPanel.Append(new UIImage(Ass.Reset.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });
        titlePanel.Append(refreshPanel);

        // Refresh debug
        

        // Assets
        pvpAsset = Main.Assets.Request<Texture2D>("Images/UI/PVP_1");
        pvpAssetHover = Main.Assets.Request<Texture2D>("Images/UI/PVP_2");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        UpdateDragging();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player.active && (players == null || !players.Contains(i)))
            {
                players ??= [];
                players.Add(i);
                needsRebuild = true;
            }
        }

        if (needsRebuild)
        {
            needsRebuild = false;

            RemoveAllChildren();

            if (pvpAsset == null || pvpAssetHover == null)
                return;

            if (titlePanel != null)
                Append(titlePanel);

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
                Top = new StyleDimension(40, 0),
                Left = new StyleDimension(0, 0),
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
            Append(summaryRow);

            var panelDims = GetDimensions();
            int spacing = 4;
            float totalIconsWidth = iconCount * iconWidth + (iconCount - 1) * spacing;
            int iconsLeft = (int)(panelDims.Width - totalIconsWidth - 10); 

            int rowTopStart = 40 + 20;

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
                        Left = new StyleDimension(0, 0),
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

                    Append(playerRow);
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

            if (x < 0) x = 0;
            else if (x + w > Main.screenWidth) x = Main.screenWidth - w;

            if (y < 0) y = 0;
            else if (y + h > Main.screenHeight) y = Main.screenHeight - h;

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
        dragging = true;
        dragOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        dragging = false;
        Recalculate();
    }
}

internal class TeamIconElement : UIElement
{
    private readonly Texture2D sheet;
    private readonly Texture2D hover;
    private readonly Rectangle src;
    private readonly Player player;
    private readonly int teamIndex;

    public TeamIconElement(Asset<Texture2D> sheetAsset, Asset<Texture2D> hoverAsset, Rectangle src, Player player, int teamIndex)
    {
        sheet = sheetAsset.Value;
        hover = hoverAsset.Value;
        this.src = src;
        this.player = player;
        this.teamIndex = teamIndex;

        Width.Set(src.Width, 0f);
        Height.Set(src.Height, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var d = GetDimensions();
        var dest = new Rectangle((int)d.X, (int)d.Y, src.Width, src.Height);

        spriteBatch.Draw(sheet, dest, src, Color.White);

        bool selected = player.team == teamIndex;
        if (IsMouseHovering || selected)
        {
            int hoverSize = hover.Width;
            var hoverDest = new Rectangle(
                dest.Center.X - hoverSize / 2,
                dest.Center.Y - hoverSize / 2,
                hoverSize,
                hoverSize
            );
            spriteBatch.Draw(hover, hoverDest, Color.White);
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (teamIndex < 0 || teamIndex >= TeamSelectorPanel.TeamColors.Length)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            new AdventurePlayer.Team((byte)player.whoAmI, (Terraria.Enums.Team)teamIndex).Serialize(packet);
            packet.Send();

            player.team = teamIndex;

            if (Parent is UIPanel row)
                row.BackgroundColor = TeamSelectorPanel.TeamColors[teamIndex];

            if (Parent?.Parent is TeamSelectorPanel panel)
                panel.needsRebuild = true;

            return;
        }

        // singleplayer / server-side local change
        player.team = teamIndex;

        if (Parent is UIPanel row2)
            row2.BackgroundColor = TeamSelectorPanel.TeamColors[teamIndex];

        if (Main.netMode == NetmodeID.Server)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            new AdventurePlayer.Team((byte)player.whoAmI, (Terraria.Enums.Team)teamIndex).Serialize(packet);
            packet.Send(-1, player.whoAmI);
        }
    }

}
