using Microsoft.Xna.Framework;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

public class UIMatchRow : UIPanel
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

    private bool selected;
    private bool hovering;

    public UIMatchRow(int index, MatchResult result)
    {
        Index = index;

        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BorderColor = new Color(89, 116, 213) * 0.7f;

        (string winnerText, Color winnerColor, int rank) = BuildWinnerLabel(result);
        Append(new UIText(winnerText, 1.05f)
        {
            Top = new StyleDimension(-5f, 0f),
            Left = new StyleDimension(50f, 0f),
            //HAlign = 0.5f,
            TextColor = winnerColor
        });

        Append(new UITeamPoints(result.TeamPoints)
        {
            VAlign = 1f,
            Top = new StyleDimension(20, 0f),
            HAlign = 0.5f
        });

        Append(new UIText(result.ToDaysAgoText(), 0.7f)
        {
            Top = new StyleDimension(18, 0),
            Left = new StyleDimension(50f, 0f),
            TextOriginX = 0,
            TextOriginY = 0,
        });

        // Add trophy if won
        //if (result.Win)
        //{
        //    Append(new UIImage(Ass.Icon_Trophy)
        //    {
        //        ImageScale = 1.10f,
        //        Left = new StyleDimension(-4, 0),
        //        Top = new StyleDimension(-10, 0)
        //    });
        //}
        var medal = rank switch
        {
            1 => Ass.Icon_Gold,
            2 => Ass.Icon_Silver,
            3 => Ass.Icon_Bronze,
            _ => TextureAssets.MagicPixel
        };

        if (medal != null && rank <= 3)
        {
            Append(new UIImage(medal)
            {
                ImageScale = 0.45f,
                Left = new StyleDimension(-16, 0),
                Top = new StyleDimension(-36, 0)
            });
            //Append(new UIText(rank.ToString(), 0.4f, true)
            //{
            //    Left = new StyleDimension(16, 0),
            //    Top = new StyleDimension(14, 0)
            //});
        }

        OnMouseOver += (_, _) =>
        {
            hovering = true;
            ApplyColors();
        };

        OnMouseOut += (_, _) =>
        {
            hovering = false;
            ApplyColors();
        };

        OnLeftClick += (_, _) => OnClick?.Invoke(Index);
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
    private static (string Text, Color Color, int Rank) BuildWinnerLabel(MatchResult result)
    {
        Team localTeam = Team.None;
        ulong localSteamId = result.LocalSteamId;

        PlayerKD[] players = result.Players ?? [];
        for (int i = 0; i < players.Length; i++)
            if (players[i].SteamId == localSteamId)
            {
                localTeam = players[i].Team;
                break;
            }

        if (localTeam == Team.None)
            return ("Placed ? (No Team)", Color.White, 0);

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
            return ("Placed ? (No Result)", Color.White, 0);

        points.Sort((a, b) => b.CompareTo(a));

        List<int> distinct = [];
        for (int i = 0; i < points.Count; i++)
            if (i == 0 || points[i] != points[i - 1])
                distinct.Add(points[i]);

        int rank = 1;
        for (int i = 0; i < distinct.Count; i++)
            if (distinct[i] == localPoints)
            {
                rank = i + 1;
                break;
            }

        static string Ordinal(int n)
        {
            int mod100 = n % 100;
            if (mod100 == 11 || mod100 == 12 || mod100 == 13) return n + "th";
            int mod10 = n % 10;
            if (mod10 == 1) return n + "st";
            if (mod10 == 2) return n + "nd";
            if (mod10 == 3) return n + "rd";
            return n + "th";
        }

        string text = $"Placed {Ordinal(rank)}";
        Color c = Main.teamColor[(int)localTeam];
        c.A = 255;
        return (text, c, rank);
    }
}
