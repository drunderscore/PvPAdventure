using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

/// <summary>
/// The main state for TPVPA match history, stats, and achievements.
/// </summary>
public sealed class MatchHistoryUIState : ResizableUIState
{
    // Content
    private UIElement matchesBaseElement = null!;
    private UIList matchList = null!; // left side list
    private UIPanel detailsPanel = null!; // right side panel
    private UITeamStatsDetails teamStatsPanel = null!;

    // Lists
    private readonly List<MatchResult> matches = [];
    private readonly List<UIMatchRow> rows = [];

    private int selectedIndex = -1; // Currently selected match

    public override void OnInitialize()
    {
        // Base element
        matchesBaseElement = new()
        {
            Width = new StyleDimension(0f, 0.8f),
            Height = new StyleDimension(0f, 1.0f),
            Top = new StyleDimension(170f, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(700f, 0f),
            MaxWidth = new StyleDimension(1100, 0f),
        };
        Append(matchesBaseElement);

        UIPanel backPanel = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        matchesBaseElement.Append(backPanel);

        backPanel.Append(new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.MainMenu.MatchHistory"), 0.9f, true)
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
            Width = new StyleDimension(0f, 0.25f),
            Height = StyleDimension.Fill
        };
        content.Append(left);

        UIElement right = new()
        {
            Width = new StyleDimension(-6f, 0.75f),
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

        // Back button
        var backButton = new UIBackButton<LocalizedText>(Language.GetText("UI.Back"), () =>
        {
            MainMenuTPVPAUIState.OpenState(() => new MainMenuTPVPAUIState());
        })
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            VAlign = 1f,
            HAlign = 0.5f,
            Width = StyleDimension.Fill,
            Left = new StyleDimension(0f, 0f)
        };
        matchesBaseElement.Append(backButton);
    }
    public override void OnActivate()
    {
        base.OnActivate();

        matches.Clear();

        // Real load later:
        // string folder = MatchStorage.GetFolderPath();
        // matches.AddRange(MatchStorage.LoadMatchesFromFolder(folder));

        // Example data for now:
        matches.Add(CreateExampleMatch());

        matchList.Clear();
        rows.Clear();

        if (matches.Count == 0)
        {
            UIElement emptyRow = new();
            emptyRow.Width.Set(0f, 1f);
            emptyRow.Height.Set(100, 0f);

            UIText emptyText = new("No matches found! Play an official TPVPA match to see the match results here.", 0.85f)
            {
                IsWrapped = true,
                HAlign = 0.5f,
                VAlign = 0.5f
            };
            emptyText.Width.Set(-24f, 1f);
            emptyRow.Append(emptyText);

            matchList.Add(emptyRow);
            matchList.Recalculate();
            return;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var row = new UIMatchRow(i, matches[i]);
            row.Width.Set(0f, 1f);
            row.Height.Set(78.29f, 0f);
            row.OnClick += SelectMatchAndRebuild;

            rows.Add(row);
            matchList.Add(row);
        }

        matchList.Recalculate();
        SelectMatchAndRebuild(0);
    }

    private static MatchResult CreateExampleMatch()
    {
        ulong localSteamId = 76561198000000001UL;

        DateTime end = DateTime.Now.AddMinutes(-23);
        DateTime start = end.AddMinutes(-17).AddSeconds(-42);

        return new MatchResult(
            start,
            end,
            win: true,
            localSteamId,
            [
                new TeamPoints(Team.Red, 230),
            new TeamPoints(Team.Blue, 180)
            ],
            [
                new PlayerKD(Team.Red, localSteamId, "Erky", 18, 7),
            new PlayerKD(Team.Red, 76561198000000002UL, "BlueMage", 11, 9),
            new PlayerKD(Team.Blue, 76561198000000003UL, "TrainHorn", 10, 13),
            new PlayerKD(Team.Blue, 76561198000000004UL, "VineBoom", 7, 17)
            ],
            [
                new TeamBossCompletion(4, Team.Red),
            new TeamBossCompletion(13, Team.Blue)
            ]);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

        // Exit state when pressing enter
        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            MainMenuTPVPAUIState.OpenState(() => new MainMenuTPVPAUIState());
        }
    }

    private void SelectMatchAndRebuild(int index)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);

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

        topRow.Append(new UIText($"Date:\n{match.Start:yyyy-MM-dd hh:mm tt}", 1.0f)
        {
            Width = new StyleDimension(0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0f, 0f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        topRow.Append(new UIText($"Result:\n{(BuildPlacementLabel(match))}", 1.0f)
        {
            Width = new StyleDimension(-0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0, 1f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        topRow.Append(new UIText($"Duration:\n{match.ToDurationDetailsText()}", 1.0f)
        {
            Width = new StyleDimension(-0, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0 * 2f, 2f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        y += topHeight + gap;

        detailsPanel.Append(new UIHorizontalSeparator(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f),
            Width = new StyleDimension(0, 1)
        });

        y += bigGap;

        // Team points, placement, and player k/d's
        TeamPoints[] teamPoints = match.TeamPoints ?? [];
        PlayerKD[] kd = match.Players ?? [];
        ulong localSteamId = match.LocalSteamId;
        teamStatsPanel = new UITeamStatsDetails(teamPoints, kd, localSteamId)
        {
            Width = new StyleDimension(4, 1),
            MaxWidth = new StyleDimension(4, 1),
            Left = new StyleDimension(-2, 0),
            Top = new StyleDimension(y, 0f)
        };
        teamStatsPanel.Height.Set(teamStatsPanel.RequiredHeight, 0f);
        detailsPanel.Append(teamStatsPanel);

        // advance y by the actual height
        y += teamStatsPanel.RequiredHeight + bigGap;

        detailsPanel.Append(new UIHorizontalSeparator(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f),
            Width = new StyleDimension(0,1)
        });

        y += 2 + bigGap;

        detailsPanel.Append(new UITeamBossCompletion(match.BossScoreboard ?? [])
        {
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(bossHeight, 0f),
            Top = new StyleDimension(y, 0f),
            HAlign = 0.5f
        });

        detailsPanel.Recalculate();
    }
    
    private static string BuildPlacementLabel(MatchResult result)
    {
        Team localTeam = Team.None;
        ulong localSteamId = result.LocalSteamId;

        PlayerKD[] players = result.Players ?? [];
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].SteamId == localSteamId)
            {
                localTeam = players[i].Team;
                break;
            }
        }

        if (localTeam == Team.None)
            return ("No result");

        TeamPoints[] pts = result.TeamPoints ?? [];
        int localPoints = int.MinValue;
        List<int> points = [];

        for (int i = 0; i < pts.Length; i++)
        {
            Team team = pts[i].Team;
            if (team == Team.None)
                continue;

            int p = pts[i].Points;
            points.Add(p);

            if (team == localTeam)
                localPoints = p;
        }

        if (localPoints == int.MinValue || points.Count == 0)
            return ("No result");

        points.Sort((a, b) => b.CompareTo(a));

        List<int> distinct = [];
        for (int i = 0; i < points.Count; i++)
        {
            if (i == 0 || points[i] != points[i - 1])
                distinct.Add(points[i]);
        }

        int rank = 1;
        for (int i = 0; i < distinct.Count; i++)
        {
            if (distinct[i] == localPoints)
            {
                rank = i + 1;
                break;
            }
        }

        int tieCount = 0;
        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i].Team != Team.None && pts[i].Points == localPoints)
                tieCount++;
        }

        string text;
        if (tieCount > 1)
            text = $"Tied for {Ordinal(rank)}";
        else
            text = $"Your team placed {Ordinal(rank)}";

        return text;
    }

    private static string Ordinal(int n)
    {
        int mod100 = n % 100;
        if (mod100 == 11 || mod100 == 12 || mod100 == 13)
            return n + "th";

        int mod10 = n % 10;
        if (mod10 == 1)
            return n + "st";
        if (mod10 == 2)
            return n + "nd";
        if (mod10 == 3)
            return n + "rd";

        return n + "th";
    }

}
