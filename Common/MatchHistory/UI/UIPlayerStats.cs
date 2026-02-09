using Microsoft.Xna.Framework;
using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

// A panel floating to the left of match history.
// Includes player stats such as k/d and win/loss ratio separated into tabs.
internal class UIPlayerStats : UIPanel
{
    private readonly UIElement content;
    private readonly UISelectableTextPanel<string>[] tabs;
    private readonly UIText[] pages;
    private int selected;

    public UIPlayerStats()
    {
        BackgroundColor = new Color(33, 43, 79) * 0.8f;
        BorderColor = Color.Black;

        var top = new UIElement
        {
            Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
            Height = StyleDimension.FromPixels(50f)
        };
        top.SetPadding(0f);
        top.PaddingTop = 4f;
        Append(top);

        top.Append(new UIHorizontalSeparator
        {
            Width = StyleDimension.FromPixelsAndPercent(-20f, 1f),
            Top = StyleDimension.FromPixels(16f),
            VAlign = 1f,
            HAlign = 0.5f,
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });

        content = new UIElement
        {
            Top = StyleDimension.FromPixels(56f),
            Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
            Height = StyleDimension.FromPixelsAndPercent(-62f, 1f)
        };
        content.SetPadding(0f);
        Append(content);

        const int tabCount = 2;
        const float outerPad = 10f;
        const float gap = 6f;

        tabs = new UISelectableTextPanel<string>[tabCount];
        pages = new UIText[tabCount];

        for (int i = 0; i < tabCount; i++)
        {
            int idx = i;

            var tab = new UISelectableTextPanel<string>("", 0.7f, true)
            {
                Width = StyleDimension.FromPixelsAndPercent(-(outerPad + gap * 0.5f), 0.5f),
                Height = StyleDimension.FromPixels(46f)
            };
            tab.SetPadding(0f);
            tab.Top.Set(6f, 0f);
            tab.Left.Set(i == 0 ? outerPad : gap * 0.5f, i == 0 ? 0f : 0.5f);

            tab.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            tab.BorderColor = Color.Black;

            tab.OnMouseOver += (_, _) =>
            {
                if (selected == idx) return;
                SoundEngine.PlaySound(12);
                tab.BackgroundColor = new Color(73, 94, 171);
                tab.BorderColor = Colors.FancyUIFatButtonMouseOver;
            };

            tab.OnMouseOut += (_, _) =>
            {
                if (selected == idx) return;
                tab.BackgroundColor = new Color(63, 82, 151) * 0.8f;
                tab.BorderColor = Color.Black;
            };

            tab.OnLeftMouseDown += (_, _) =>
            {
                SoundEngine.PlaySound(12);
                Select(idx);
            };

            tab.Append(new UIImage(i == 0 ? Ass.Icon_Attack : Ass.Icon_Trophy)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                ImageScale = i == 0 ? 1.0f : 0.8f
            });

            top.Append(tab);
            tabs[idx] = tab;

            pages[idx] = new UIText("", 0.9f)
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill,
                TextOriginX = 0f,
                TextOriginY = 0f,
                IsWrapped = false,
                PaddingLeft = 10f,
                PaddingTop = 16f
            };
        }

        SetStats(0, 0, 0, 0, 0);
        Select(0);
    }

    private void Select(int idx)
    {
        selected = idx;
        content.RemoveAllChildren();
        content.Append(pages[idx]);

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = i == idx;
            tabs[i].BackgroundColor = isSelected ? new Color(73, 94, 171) : new Color(63, 82, 151) * 0.8f;
            tabs[i].BorderColor = isSelected ? Colors.FancyUIFatButtonMouseOver : Color.Black;
        }
    }

    public void Update(IReadOnlyList<MatchResult> matches, ulong steamUserId)
    {
        int kills = 0, deaths = 0, wins = 0, losses = 0, teamPointsTotal = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            MatchResult m = matches[i];
            if (m.Win) wins++; else losses++;

            ulong myId = steamUserId != 0 ? steamUserId : (ulong)m.LocalSteamId;
            if (myId == 0)
                continue;

            Team myTeam = Team.None;

            PlayerKD[] players = m.Players ?? [];
            for (int j = 0; j < players.Length; j++)
            {
                if ((ulong)players[j].SteamId != myId)
                    continue;

                kills += players[j].Kills;
                deaths += players[j].Deaths;
                myTeam = players[j].Team;
                break;
            }

            if (myTeam == Team.None)
                continue;

            TeamPoints[] pts = m.TeamPoints ?? [];
            for (int j = 0; j < pts.Length; j++)
            {
                if (pts[j].Team != myTeam)
                    continue;

                teamPointsTotal += pts[j].Points;
                break;
            }
        }

        SetStats(kills, deaths, wins, losses, teamPointsTotal);
    }

    public void SetStats(int kills, int deaths, int wins, int losses, int teamPointsTotal)
    {
        static string Ratio(int a, int b) => b <= 0 ? (a <= 0 ? "0" : "INF") : ((float)a / b).ToString("0.00");

        pages[0].SetText($"Total K/D: {Ratio(kills, deaths)}\nTotal Kills: {kills}\nTotal Deaths: {deaths}");
        pages[1].SetText($"Total W/L: {Ratio(wins, losses)}\nTotal Wins: {wins}\nTotal Losses: {losses}\nTotal Points: {teamPointsTotal}");
    }
}
