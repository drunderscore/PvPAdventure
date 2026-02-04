using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Utilities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MatchHistory.UI;

public sealed class MatchHistoryUIState : ResizableUIState
{
    // Content
    private UIList matchList = null!; // left side list
    private UIText detailsText = null!; // right side details text
    private UIPanel detailsPanel = null!; // right side panel
    private DownloadUIButton downloadButton = null!;
    private UITeamStats teamStatsPanel = null!;
    private UIPlayerStats playerStats = null!;

    // Details text for each match
    private UIText dateText = null!;
    private UIText resultText = null!;
    private UIText durationText = null!;

    // Lists
    private readonly List<MatchResult> matches = [];
    private readonly List<MatchRow> rows = [];

    private int selectedIndex = -1; // Currently selected match

    public override void OnInitialize()
    {
        playerStats = new("Total kills: {}\nTotal deaths: {}\nTotal wins: {}\nTotal losses: {}")
        {
            Top = new StyleDimension(200,0),
            Left = new StyleDimension(100,0)
        };
        Append(playerStats);

        UIElement baseElement = new()
        {
            Width = new StyleDimension(0f, 0.8f),
            Height = new StyleDimension(0f, 1f),
            Top = new StyleDimension(160f, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(700f, 0f),
            MaxWidth = new StyleDimension(900f, 0f),
        };
        Append(baseElement);

        UIPanel backPanel = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        baseElement.Append(backPanel);

        backPanel.Append(new UITextPanel<string>("TPVPA Match History", 0.9f, true)
        {
            Top = new StyleDimension(-48f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        UIElement content = new()
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f)
        };
        backPanel.Append(content);

        UIElement left = new()
        {
            Width = new StyleDimension(0f, 0.35f),
            Height = StyleDimension.Fill
        };
        content.Append(left);

        UIElement right = new()
        {
            Width = new StyleDimension(-6f, 0.65f),
            Height = StyleDimension.Fill,
            HAlign = 1f
        };
        content.Append(right);

        UIScrollbar matchScrollbar = new()
        {
            Top = new StyleDimension(10f, 0f),
            Height = new StyleDimension(-20f, 1f),
            HAlign = 1f
        };
        left.Append(matchScrollbar);

        UIElement listContainer = new()
        {
            Top = new StyleDimension(6f, 0f),
            Width = new StyleDimension(-matchScrollbar.Width.Pixels - 6f, 1f),
            Height = new StyleDimension(-12f, 1f)
        };
        left.Append(listContainer);

        matchList = new UIList
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            ListPadding = 6f,
            ManualSortMethod = _ => { }
        };
        matchList.SetScrollbar(matchScrollbar);
        listContainer.Append(matchList);

        detailsPanel = new UIPanel
        {
            Top = new StyleDimension(6f, 0f),
            Left = new StyleDimension(6f, 0f),
            Width = new StyleDimension(-12f, 1f),
            Height = new StyleDimension(-12f, 1f),
            BackgroundColor = UICommon.DefaultUIBlueMouseOver
        };
        right.Append(detailsPanel);

        baseElement.Append(new ActionUIButton<LocalizedText>(Language.GetText("UI.Back"), () =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.MenuUI.SetState(null);
            Main.menuMode = 0;
        })
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            VAlign = 1f
        });

        downloadButton = new DownloadUIButton(
        onClick: () =>
        {
            DateTime start = matches[selectedIndex].Start;
            string folder = Path.Combine(Main.SavePath, "PvPAdventure", "MatchHistory");
            DownloadUIButton.OpenMatchFile(folder, start, MatchJsonStorage.GetMatchFilePath);
        },
        getDownloadedLabel: () => "" )
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            HAlign = 1f,
            VAlign = 1f
        }; 
        
        baseElement.Append(downloadButton);
    }
    public override void OnActivate()
    {
        base.OnActivate();

        string folder = Path.Combine(Main.SavePath, "PvPAdventure", "MatchHistory");
        matches.Clear();
        matches.AddRange(MatchJsonLoader.LoadMatchesFromFolder(folder));

        BuildList();

        ulong steamId = 0;
        try
        {
            steamId = SteamUser.GetSteamID().m_SteamID;
        }
        catch
        {
            steamId = 0;
        }

        playerStats.Update(matches, steamId);

        if (matches.Count > 0)
            SelectIndexAndRebuild(0);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.MenuUI.SetState(null);
            Main.menuMode = 0;
        }
    }

    private void BuildList()
    {
        matchList.Clear();
        rows.Clear();

        for (int i = 0; i < matches.Count; i++)
        {
            var row = new MatchRow(i, matches[i]);
            row.Width.Set(0f, 1f);
            row.Height.Set(78.29f, 0f);
            row.OnClick += SelectIndexAndRebuild;

            rows.Add(row);
            matchList.Add(row);
        }

        matchList.Recalculate();
    }

    private void SelectIndexAndRebuild(int index)
    {
        if (matches.Count == 0)
            return;

        if (index < 0)
            index = 0;

        if (index >= matches.Count)
            index = matches.Count - 1;

        selectedIndex = index;

        for (int i = 0; i < rows.Count; i++)
            rows[i].Selected = i == selectedIndex;

        detailsPanel.RemoveAllChildren();

        const float topHeight = 50f;
        const float statsHeight = 170f;
        const float bossHeight = 150f;
        const float gap = 6f;
        const float bigGap = gap * 4;

        MatchResult match = matches[selectedIndex];

        float y = 6;

        UIElement topRow = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(topHeight, 0f),
            Top = new StyleDimension(y, 0f)
        };
        detailsPanel.Append(topRow);

        dateText = new UIText($"Date:\n{match.Start:yyyy-MM-dd hh:mm tt}", 0.8f)
        {
            Width = new StyleDimension(0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0f, 0f),
            TextOriginX = 0.5f,
            IsWrapped = false
        };
        topRow.Append(dateText);

        resultText = new UIText($"Result:\n{(match.Win ? "Win" : "Loss")}", 0.8f)
        {
            Width = new StyleDimension(-0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0, 1f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        };
        topRow.Append(resultText);

        durationText = new UIText($"Duration:\n{match.ToDurationDetailsText()}", 0.8f)
        {
            Width = new StyleDimension(-0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0 * 2f, 2f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        };
        topRow.Append(durationText);

        y += topHeight + gap;

        detailsPanel.Append(new UIHorizontalSeparator(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f),
            Width = new StyleDimension(0, 1)
        });

        y += bigGap;

        TeamPoints[] teamPoints = match.TeamPoints ?? [];
        PlayerKD[] kd = match.Players ?? [];
        ulong localSteamId = match.LocalSteamId;

        teamStatsPanel = new UITeamStats(teamPoints, kd, localSteamId)
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(statsHeight, 0f),
            Top = new StyleDimension(y, 0f)
        };
        detailsPanel.Append(teamStatsPanel);

        y += statsHeight + bigGap;

        UIHorizontalSeparator sep2 = new(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f)
        };
        sep2.Width.Set(0f, 1f);
        detailsPanel.Append(sep2);

        y += sep2.Height.Pixels + bigGap;

        var bossUi = new UITeamBossCompletion(match.BossScoreboard ?? [])
        {
            Width = new StyleDimension(10f, 1f),
            MaxWidth = new StyleDimension(10f, 1f),
            Left = new StyleDimension(-5f, 0f),
            Height = new StyleDimension(bossHeight, 0f),
            Top = new StyleDimension(y, 0f)
        };
        detailsPanel.Append(bossUi);

        detailsPanel.Recalculate();
        downloadButton.RefreshLabel();
    }

    private sealed class MatchRow : UIPanel
    {
        public event Action<int>? OnClick;

        public int Index { get; }

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                ApplyColors();
            }
        }

        private readonly UIText text;
        private readonly UIText resultText;
        private bool selected;
        private bool hovering;

        public MatchRow(int index, MatchResult result)
        {
            Index = index;

            // UI properties
            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            // x team wins text
            Index = index;

            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            TeamPoints[] pts = result.TeamPoints ?? [];
            int max = int.MinValue;

            for (int i = 0; i < pts.Length; i++)
                if (pts[i].Team != Team.None && pts[i].Points > max)
                    max = pts[i].Points;

            Team[] winners = pts
                .Where(tp => tp.Team != Team.None && tp.Points == max)
                .Select(tp => tp.Team)
                .OrderBy(t => (int)t)
                .ToArray();

            if (winners.Length > 0)
            {
                string msg = winners.Length == 1
                    ? $"{TeamName(winners[0])}\nteam\nwins!"
                    : $"{string.Join(" & ", winners.Select(TeamName))}\ntie!";

                Color c = Color.White;
                if (winners.Length == 1)
                {
                    c = Main.teamColor[(int)winners[0]];
                    c.A = 255;
                }

                //Append(new UIText(msg, 0.8f)
                //{
                //    Top = new StyleDimension(-20f, 1f),
                //    Left = new StyleDimension(6f, 0f),
                //    Width = StyleDimension.Fill,
                //    Height = StyleDimension.Fill,
                //    TextOriginX = 0f,
                //    TextOriginY = 1f,
                //    TextColor = c,
                //    IsWrapped = false
                //});
            }

            // Days ago text
            Append(new UIText(result.ToDaysAgoText(), 0.9f)
            {
                Top = new StyleDimension(-20, 1),
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill,
                TextOriginX = 0.5f,
                TextOriginY = 1.0f,
                IsWrapped = true
            });

            // Team points panels
            Append(new UITeamPointsPanel(result.TeamPoints)
            {
                VAlign = 0f,
                Top = new StyleDimension(4, 0f),
                HAlign = 0.5f
            });

            //Add a trophy if win
            //if (result.Win)
            //{
            //    UIImage trophy = new(Ass.Icon_Trophy);
            //    trophy.Left.Set(-42, 1);
            //    trophy.Top.Set(-60, 0);
            //    Append(trophy);
            //    Append(new UIText("Winner!", 0.8f)
            //    {
            //        Top = new StyleDimension(-20, 1),
            //        Left = new StyleDimension(6, 0),
            //        Width = StyleDimension.Fill,
            //        Height = StyleDimension.Fill,
            //        TextOriginX = 1.0f,
            //        TextOriginY = 1.0f,
            //        IsWrapped = true
            //    });
            //}
        }

        private static string TeamName(Team t) => t.ToString();

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            bool nowHovering = IsMouseHovering;
            if (nowHovering != hovering)
            {
                hovering = nowHovering;
                ApplyColors();
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            OnClick?.Invoke(Index);
        }

        private void ApplyColors()
        {
            if (Selected)
            {
                BackgroundColor = new Color(73, 94, 171);
                BorderColor = Colors.FancyUIFatButtonMouseOver;
                return;
            }

            if (hovering)
            {
                BackgroundColor = new Color(73, 94, 171);
                BorderColor = new Color(89, 116, 213);
                return;
            }

            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;
        }
    }

}
