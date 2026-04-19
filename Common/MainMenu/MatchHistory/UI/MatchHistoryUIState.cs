using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.MatchHistory.UI;

/// <summary>
/// The main state for TPVPA match history
/// </summary>
public sealed class MatchHistoryUIState : MainMenuPageUIState
{
    private UIList matchList = null!;
    private UIPanel detailsPanel = null!;
    private UITeamStatsDetails teamStatsPanel = null!;

    private MatchHistoryUIContent content;
    private readonly List<UIMatchRow> rows = [];

    private int selectedIndex = -1;

    protected override float MainPanelMinWidth => 850f;
    //protected override float MainPanelTop => 170f;
    //protected override float RootMaxWidth => 1100f;
    //protected override float? RootMinWidth => 700f;
    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.MatchHistory";

    protected override void Populate(UIPanel panel)
    {
        base.Populate(panel);
        UIElement content = new()
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f)
        };
        panel.Append(content);

        float leftSidePercent = 0.3f;
        UIElement left = new()
        {
            Width = new StyleDimension(0f, leftSidePercent),
            Height = StyleDimension.Fill
        };
        content.Append(left);

        UIElement right = new()
        {
            Width = new StyleDimension(0f, 1f - leftSidePercent),
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
    }

    protected override void RefreshContent()
    {
        selectedIndex = -1;

        rows.Clear();
        matchList.Clear();
        detailsPanel.RemoveAllChildren();

        SetCurrentAsyncState(AsyncProviderState.Loading);
        bool buildExampleContent = true;
        content = buildExampleContent
            ? MatchHistoryExampleContent.Create()
            : new MatchHistoryUIContent([]);

        if (!buildExampleContent)
        {
            // TODO: Call the match history API here and map the response into MatchHistoryUIContent.
        }

        RebuildMatchUi();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void RebuildMatchUi()
    {
        matchList.Clear();
        rows.Clear();
        detailsPanel.RemoveAllChildren();

        if (content.Matches.Length == 0)
        {
            ShowEmptyState();
            return;
        }

        for (int i = 0; i < content.Matches.Length; i++)
        {
            UIMatchRow row = new(i, content.Matches[i]);
            row.Width.Set(0f, 1f);
            row.Height.Set(78.29f, 0f);
            row.OnClick += SelectMatchAndRebuild;

            rows.Add(row);
            matchList.Add(row);
        }

        matchList.Recalculate();
        SelectMatchAndRebuild(0);
    }

    private void ShowLoadingState()
    {
        MainMenuPageUIState.ShowWrappedMessage(detailsPanel, MainMenuPageUIState.FormatLoadingMessage("match history"));
    }

    private void ShowEmptyState()
    {
        MainMenuPageUIState.ShowWrappedMessage(detailsPanel, "No matches available.\nPlay an official TPVPA match to see the match results here.");
    }

    private void ShowErrorState(string message)
    {
        matchList.Clear();
        rows.Clear();
        MainMenuPageUIState.ShowWrappedMessage(detailsPanel, message);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
    }

    private void SelectMatchAndRebuild(int index)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);

        if (content.Matches.Length == 0)
            return;

        if (index < 0)
            index = 0;

        if (index >= content.Matches.Length)
            index = content.Matches.Length - 1;

        selectedIndex = index;

        for (int i = 0; i < rows.Count; i++)
            rows[i].Selected = i == selectedIndex;

        detailsPanel.RemoveAllChildren();

        const float topHeight = 50f;
        const float bossHeight = 150f;
        const float gap = 6f;
        const float bigGap = gap * 4;

        MatchResult match = content.Matches[selectedIndex];

        float y = 6f;

        UIElement topRow = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(topHeight, 0f),
            Top = new StyleDimension(y, 0f)
        };
        detailsPanel.Append(topRow);

        topRow.Append(new UIText($"Date:\n{match.Start:yyyy-MM-dd hh:mm tt}", 1f)
        {
            Width = new StyleDimension(0f, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0f, 0f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        topRow.Append(new UIText($"Result:\n{BuildPlacementLabel(match)}", 1f)
        {
            Width = new StyleDimension(0f, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0f, 1f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        topRow.Append(new UIText($"Duration:\n{match.ToDurationDetailsText()}", 1f)
        {
            Width = new StyleDimension(0f, 1f / 3f),
            Height = StyleDimension.Fill,
            Left = new StyleDimension(0f, 2f / 3f),
            TextOriginX = 0.5f,
            IsWrapped = false
        });

        y += topHeight + gap;

        detailsPanel.Append(new UIHorizontalSeparator(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f),
            Width = new StyleDimension(0f, 1f)
        });

        y += bigGap;

        TeamPoints[] teamPoints = match.TeamPoints ?? [];
        PlayerKD[] kd = match.Players ?? [];
        ulong localSteamId = match.LocalSteamId;

        teamStatsPanel = new UITeamStatsDetails(teamPoints, kd, localSteamId)
        {
            Width = new StyleDimension(4f, 1f),
            MaxWidth = new StyleDimension(4f, 1f),
            Left = new StyleDimension(-2f, 0f),
            Top = new StyleDimension(y, 0f)
        };
        teamStatsPanel.Height.Set(teamStatsPanel.RequiredHeight, 0f);
        detailsPanel.Append(teamStatsPanel);

        y += teamStatsPanel.RequiredHeight + bigGap;

        detailsPanel.Append(new UIHorizontalSeparator(2, highlightSideUp: true)
        {
            Top = new StyleDimension(y, 0f),
            Width = new StyleDimension(0f, 1f)
        });

        y += 2f + bigGap;

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
            return "No result";

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
            return "No result";

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

        return tieCount > 1 ? $"Tied for {Ordinal(rank)}" : $"Your team placed {Ordinal(rank)}";
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
