using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.Social.Steam;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

public sealed class UITeamStats : UIElement
{
    private readonly ulong _localSteamId;
    private readonly List<UIPanel> _teamPanels = [];
    private float _lastW = -1f;
    private float _lastH = -1f;

    public UITeamStats(TeamPoints[] teamPoints, PlayerKD[] players, ulong localSteamId)
    {
        _localSteamId = localSteamId;

        Width = StyleDimension.Fill;
        Height = StyleDimension.Fill;

        teamPoints ??= [];
        players ??= [];

        var pointsByTeam = teamPoints
            .Where(tp => tp.Team != Team.None)
            .GroupBy(tp => tp.Team)
            .ToDictionary(g => g.Key, g => g.First().Points);

        var teams = new HashSet<Team>();

        for (int i = 0; i < teamPoints.Length; i++)
        {
            Team t = teamPoints[i].Team;
            if (t != Team.None)
                teams.Add(t);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Team t = players[i].Team;
            if (t != Team.None)
                teams.Add(t);
        }

        Team[] orderedTeams = teams.OrderBy(t => (int)t).ToArray();

        for (int i = 0; i < orderedTeams.Length; i++)
        {
            Team team = orderedTeams[i];

            int points = 0;
            if (pointsByTeam.TryGetValue(team, out int v))
                points = v;

            PlayerKD[] teamPlayers = players
                .Where(p => p.Team == team)
                .OrderByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .ThenBy(p => p.PlayerName)
                .ToArray();

            var panel = BuildTeamPanel(team, points, teamPlayers, _localSteamId);
            _teamPanels.Add(panel);
            Append(panel);
        }
    }

    public override void Recalculate()
    {
        base.Recalculate();
        LayoutColumns();
    }

    private void LayoutColumns()
    {
        CalculatedStyle dim = GetDimensions();

        float w = dim.Width;
        float h = dim.Height;

        if (Math.Abs(w - _lastW) < 0.5f && Math.Abs(h - _lastH) < 0.5f)
            return;

        _lastW = w;
        _lastH = h;

        int n = _teamPanels.Count;
        if (n <= 0)
            return;

        const float gap = 6f;
        float colW = (w - gap * (n - 1)) / n;
        if (colW < 1f)
            colW = 1f;

        float x = 0f;

        for (int i = 0; i < n; i++)
        {
            UIPanel p = _teamPanels[i];

            p.Left.Set(x, 0f);
            p.Top.Set(0f, 0f);
            p.Width.Set(colW, 0f);
            p.Height.Set(0f, 1f);

            x += colW + gap;
        }
    }

    private static UIPanel BuildTeamPanel(Team team, int points, PlayerKD[] players, ulong localSteamId)
    {
        var panel = new UIPanel
        {
            BackgroundColor = Main.teamColor[(int)team] * 0.55f,
            BorderColor = Color.Black,
            PaddingTop = 6f,
            PaddingBottom = 6f,
            PaddingLeft = 6f,
            PaddingRight = 6f
        };

        var pointsText = new UIText(FormatPoints(points), 0.45f, true)
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(20f, 0f),
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        };
        panel.Append(pointsText);

        UIList list = new()
        {
            Top = new StyleDimension(22f, 0f),
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-22f, 1f),
            ListPadding = 1f,
            ManualSortMethod = _ => { }
        };
        panel.Append(list);

        for (int i = 0; i < players.Length; i++)
            list.Add(BuildPlayerRow(players[i], localSteamId));

        return panel;
    }

    private static UIElement BuildPlayerRow(PlayerKD p, ulong localSteamId)
    {
        UIElement row = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(22f, 0f)
        };

        float kdPx = 54f;

        string nameText = p.PlayerName ?? "";

        if (localSteamId != 0 && p.SteamId == localSteamId)
            nameText += " (you)";

        var name = new UIText(nameText, 0.7f)
        {
            Left = new StyleDimension(0f, 0f),
            Width = new StyleDimension(-kdPx, 1f),
            Height = StyleDimension.Fill,
            VAlign = 0.5f,
            TextOriginX = 0f,
            TextOriginY = 0.5f
        };
        row.Append(name);

        var kd = new UIText($"{p.Kills}/{p.Deaths}", 0.7f)
        {
            Left = new StyleDimension(-kdPx - 3f, 1f),
            Width = new StyleDimension(kdPx, 0f),
            Height = StyleDimension.Fill,
            VAlign = 0.5f,
            TextOriginX = 1f,
            TextOriginY = 0.5f
        };
        row.Append(kd);

        return row;
    }

    private static string FormatPoints(int points)
    {
        if (points == 1)
            return "1 pt";

        return $"{points} pts";
    }


    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
