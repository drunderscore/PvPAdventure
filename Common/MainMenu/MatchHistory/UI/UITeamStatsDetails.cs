using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.MatchHistory;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.Social.Steam;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.MatchHistory.UI;

public sealed class UITeamStatsDetails : UIElement
{
    private const int MaxPlayersPerTeam = 12;

    private const float Gap = 6f;

    private const float PanelPad = 6f;
    private const float HeaderH = 18f;
    private const float PointsH = 20f;
    private const float BetweenHeaderAndList = 2f;

    private const float RowH = 22f;
    private const float ListPad = 1f;

    private readonly ulong _localSteamId;
    private readonly List<UIPanel> _teamPanels = [];
    private float _lastW = -1f;
    private float _lastH = -1f;

    public float RequiredHeight { get; private set; }

    public UITeamStatsDetails(TeamPoints[] teamPoints, PlayerKD[] players, ulong localSteamId)
    {
        _localSteamId = localSteamId;

        Width = StyleDimension.Fill;

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

        var ranked = orderedTeams
            .Select(t => (Team: t, Points: pointsByTeam.TryGetValue(t, out int p) ? p : 0))
            .OrderByDescending(x => x.Points)
            .ThenBy(x => (int)x.Team)
            .ToArray();

        Dictionary<Team, int> placeByTeam = [];
        Dictionary<Team, bool> tieByTeam = [];

        int place = 1;
        int prevPoints = int.MinValue;

        for (int i = 0; i < ranked.Length; i++)
        {
            int pts = ranked[i].Points;

            if (i == 0)
            {
                place = 1;
            }
            else if (pts != prevPoints)
            {
                place = i + 1;
            }

            prevPoints = pts;
            placeByTeam[ranked[i].Team] = place;
        }

        for (int i = 0; i < ranked.Length; i++)
        {
            int pts = ranked[i].Points;
            int count = 0;

            for (int j = 0; j < ranked.Length; j++)
                if (ranked[j].Points == pts)
                    count++;

            tieByTeam[ranked[i].Team] = count > 1;
        }

        int maxTeamPlayers = 0;

        for (int i = 0; i < orderedTeams.Length; i++)
        {
            Team team = orderedTeams[i];

            int points = pointsByTeam.TryGetValue(team, out int v) ? v : 0;

            PlayerKD[] teamPlayers = players
                .Where(p => p.Team == team)
                .OrderByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .ThenBy(p => p.PlayerName)
                .ToArray();

            int rows = teamPlayers.Length;
            if (rows > MaxPlayersPerTeam)
                rows = MaxPlayersPerTeam;

            if (rows > maxTeamPlayers)
                maxTeamPlayers = rows;

            int placeRank = placeByTeam.TryGetValue(team, out int r) ? r : 0;
            bool tied = tieByTeam.TryGetValue(team, out bool t) && t;

            var panel = BuildTeamPanel(team, points, teamPlayers, _localSteamId, placeRank, tied);
            _teamPanels.Add(panel);
            Append(panel);
        }

        RequiredHeight = ComputeRequiredHeight(maxTeamPlayers);
        Height.Set(RequiredHeight, 0f);
    }

    public override void Recalculate()
    {
        base.Recalculate();
        LayoutColumns();
    }

    private static float ComputeRequiredHeight(int maxTeamPlayers)
    {
        if (maxTeamPlayers < 0)
            maxTeamPlayers = 0;

        if (maxTeamPlayers > MaxPlayersPerTeam)
            maxTeamPlayers = MaxPlayersPerTeam;

        float listH = 0f;

        if (maxTeamPlayers > 0)
            listH = maxTeamPlayers * RowH + (maxTeamPlayers - 1) * ListPad;

        float inner = HeaderH + PointsH + BetweenHeaderAndList + listH;
        return PanelPad + inner + PanelPad + 4;
    }

    private static UIPanel BuildTeamPanel(Team team, int points, PlayerKD[] players, ulong localSteamId, int placeRank, bool tied)
    {
        var panel = new UIPanel
        {
            BackgroundColor = Main.teamColor[(int)team] * 0.55f,
            BorderColor = Color.Black,
            PaddingTop = PanelPad,
            PaddingBottom = PanelPad,
            PaddingLeft = PanelPad,
            PaddingRight = PanelPad
        };

        var pointsText = new UIText(FormatPoints(points), 0.45f, true)
        {
            Top = new StyleDimension(0, 0f),
            Width = StyleDimension.Fill,
            Height = new StyleDimension(PointsH, 0f),
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        };
        panel.Append(pointsText);

        float listTop = PointsH + BetweenHeaderAndList;

        UIList list = new()
        {
            Top = new StyleDimension(listTop + 4, 0f),
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-listTop, 1f),
            ListPadding = ListPad,
            ManualSortMethod = _ => { }
        };
        panel.Append(list);

        int count = players.Length;
        if (count > MaxPlayersPerTeam)
            count = MaxPlayersPerTeam;

        for (int i = 0; i < count; i++)
            list.Add(BuildPlayerRow(players[i], localSteamId));

        return panel;
    }

    private static UIElement BuildPlayerRow(PlayerKD p, ulong localSteamId)
    {
        UIElement row = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(RowH, 0f)
        };

        float kdPx = 54f;

        string nameText = p.PlayerName ?? "";
        Color nameColor = new Color(220, 220, 220, 220);

        if (localSteamId != 0 && p.SteamId == localSteamId)
        {
            nameText += " (you)";
            nameColor = Color.White;
        }

        var name = new UIText(nameText, 0.7f)
        {
            Left = new StyleDimension(1f, 0f),
            Width = new StyleDimension(-kdPx, 1f),
            Height = StyleDimension.Fill,
            VAlign = 0.5f,
            TextOriginX = 0f,
            TextOriginY = 0.5f,
            TextColor = nameColor
        };
        row.Append(name);

        var kd = new UIText($"{p.Kills}/{p.Deaths}", 0.7f)
        {
            Left = new StyleDimension(-kdPx - 3f, 1f),
            Width = new StyleDimension(kdPx, 0f),
            MaxWidth = new StyleDimension(1, 12f),
            Height = StyleDimension.Fill,
            VAlign = 0.5f,
            TextOriginX = 1f,
            TextOriginY = 0.5f
        };
        row.Append(kd);

        return row;
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

        // Sort panels by points (descending, left to right)
        var sortedPanels = _teamPanels.AsEnumerable().ToList();
        sortedPanels.Sort((a, b) =>
        {
            // Get the first direct child UIText (the points text)
            var aText = a.Children.OfType<UIText>().FirstOrDefault(t => t.Text != null && t.Text.Contains("pt"));
            var bText = b.Children.OfType<UIText>().FirstOrDefault(t => t.Text != null && t.Text.Contains("pt"));

            if (aText?.Text == null || bText?.Text == null)
                return 0;

            int aPoints = ExtractPoints(aText.Text);
            int bPoints = ExtractPoints(bText.Text);

            return bPoints.CompareTo(aPoints); // Descending
        });

        float colW = (w - Gap * (n - 1)) / n;
        if (colW < 1f)
            colW = 1f;

        float x = 0f;

        for (int i = 0; i < n; i++)
        {
            UIPanel p = sortedPanels[i];

            p.Left.Set(x, 0f);
            p.Top.Set(0f, 0f);
            p.Width.Set(colW, 0f);
            p.Height.Set(0f, 1f);

            x += colW + Gap;
        }
    }

    private static int ExtractPoints(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static string FormatPoints(int points) => points == 1 ? "1 pt" : $"{points} pts";

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
