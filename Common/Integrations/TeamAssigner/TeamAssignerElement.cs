using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Integrations.SharedUI;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.TeamAssigner;

/// <summary>
/// The base element for the team assigner UI (title and content panel).
/// </summary>
internal class TeamAssignerElement : DraggablePanel
{
    // Team colors: 0–5
    internal static readonly Color[] TeamColors =
    [
        new(0xC5, 0xC1, 0xD8),
        new(0xDA, 0x3B, 0x3B),
        new(0x3B, 0xDA, 0x55),
        new(0x3B, 0x95, 0xDA),
        new(0xDA, 0xB7, 0x3B),
        new(0xE0, 0x64, 0xF2),
    ];

    public bool needsRebuild = false;

    private readonly List<int> players = new();
    private readonly int[] lastTeams = new int[Main.maxPlayers];
    private readonly bool[] lastActive = new bool[Main.maxPlayers];

    private Asset<Texture2D> pvpAsset;
    private Asset<Texture2D> pvpAssetHover;

    public TeamAssignerElement() : base(title: Language.GetTextValue("Mods.PvPAdventure.Tools.DLTeamAssignerTool.TitlePanelName"))
    {
        Width.Set(350, 0);
        Height.Set(460, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;

        pvpAsset = Main.Assets.Request<Texture2D>("Images/UI/PVP_1");
        pvpAssetHover = Main.Assets.Request<Texture2D>("Images/UI/PVP_2");

        needsRebuild = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

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
            }
        }

        if (needsRebuild)
        {
            needsRebuild = false;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        ContentPanel.RemoveAllChildren();

        if (pvpAsset?.Value == null || pvpAssetHover?.Value == null)
            return;

        Texture2D sheetTex = pvpAsset.Value;

        const int iconCount = 6;
        int iconW = sheetTex.Width / iconCount;
        int iconH = sheetTex.Height;

        int[] teamCounts = new int[iconCount];
        for (int i = 0; i < players.Count; i++)
        {
            Player p = Main.player[players[i]];
            int t = p.team;
            if (t >= 0 && t < iconCount)
                teamCounts[t]++;
        }

        // Summary row
        var summaryRow = new UIPanel
        {
            Width = { Percent = 1f },
            Height = { Pixels = 24f },
            BackgroundColor = new Color(10, 10, 10) * 0.9f,
            BorderColor = Color.Black
        };
        summaryRow.SetPadding(4f);

        string summaryText =
            $"None:{teamCounts[0]}  Red:{teamCounts[1]}  Green:{teamCounts[2]}  Blue:{teamCounts[3]}  Yellow:{teamCounts[4]}  Pink:{teamCounts[5]}";

        summaryRow.Append(new UIText(summaryText, textScale: 0.8f) { VAlign = 0.5f, HAlign = 0.5f });

        ContentPanel.Append(summaryRow);

        // Layout constants inside ContentPanel
        const int rowH = 30;
        const int rowTopStart = 24 + 6; // summary(24) + padding
        const int spacing = 4;
        const int rightPadding = 10;

        // Right aligned icons inside ContentPanel
        float totalIconsWidth = (iconCount * iconW) + ((iconCount - 1) * spacing);
        float iconsLeft = -rightPadding - totalIconsWidth;

        for (int i = 0; i < players.Count; i++)
        {
            int playerIndex = players[i];
            Player player = Main.player[playerIndex];

            int team = player.team;
            if (team < 0 || team >= TeamColors.Length)
                team = 0;

            var playerRow = new UIPanel
            {
                Top = { Pixels = rowTopStart + (i * rowH) },
                Width = { Percent = 1f },
                Height = { Pixels = rowH },
                BackgroundColor = TeamColors[team],
                BorderColor = Color.Black
            };
            playerRow.SetPadding(0f);

            string playerName = player == Main.LocalPlayer ? player.name + " (you)" : player.name;

            playerRow.Append(new UIText(playerName, textScale: 1.0f)
            {
                Left = { Pixels = 10f },
                VAlign = 0.5f
            });

            for (int teamIndex = 0; teamIndex < iconCount; teamIndex++)
            {
                var src = new Rectangle(teamIndex * iconW, 0, iconW, iconH);

                var icon = new TeamIconElement(pvpAsset, pvpAssetHover, src, player, teamIndex)
                {
                    Left = { Percent = 1f, Pixels = iconsLeft + (teamIndex * (iconW + spacing)) },
                    VAlign = 0.5f
                };

                playerRow.Append(icon);
            }

            ContentPanel.Append(playerRow);
        }

        ContentPanel.Recalculate();
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<TeamAssignerSystem>().ToggleActive();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        players.Sort((a, b) => Main.player[a].team.CompareTo(Main.player[b].team));
        needsRebuild = true;
    }

}
