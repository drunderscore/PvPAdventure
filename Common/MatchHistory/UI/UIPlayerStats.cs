using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

internal class UIPlayerStats : UIPanel
{
    private readonly string template;
    private readonly UIText text;

    public UIPlayerStats(string template, float textScale = 0.8f)
    {
        Width.Set(130, 0);
        Height.Set(100, 0);

        this.template = template;

        PaddingLeft = 8f;
        PaddingRight = 8f;
        PaddingTop = 6f;
        PaddingBottom = 6f;

        BackgroundColor = UICommon.DefaultUIBlueMouseOver;
        BorderColor = Color.Black;

        text = new UIText("", textScale)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            TextOriginX = 0f,
            TextOriginY = 0f,
            IsWrapped = false
        };
        Append(text);
    }

    public void Update(IReadOnlyList<MatchResult> matches, ulong steamUserId)
    {
        int kills = 0;
        int deaths = 0;
        int wins = 0;
        int losses = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            MatchResult m = matches[i];

            if (m.Win) wins++;
            else losses++;

            ulong myId = steamUserId != 0 ? steamUserId : (ulong)m.LocalSteamId;
            if (myId == 0)
                continue;

            PlayerKD[] players = m.Players ?? [];
            for (int j = 0; j < players.Length; j++)
            {
                ulong pid = (ulong)players[j].SteamId;
                if (pid != myId)
                    continue;

                kills += players[j].Kills;
                deaths += players[j].Deaths;
                break; // only one row should match "me" per match
            }
        }

        SetStats(kills, deaths, wins, losses);
    }

    private void SetStats(int kills, int deaths, int wins, int losses)
    {
        string s = template;

        s = ReplaceOnce(s, "{}", kills.ToString());
        s = ReplaceOnce(s, "{}", deaths.ToString());
        s = ReplaceOnce(s, "{}", wins.ToString());
        s = ReplaceOnce(s, "{}", losses.ToString());

        text.SetText(s);
    }

    private static string ReplaceOnce(string s, string find, string repl)
    {
        int i = s.IndexOf(find, StringComparison.Ordinal);
        if (i < 0)
            return s;

        return s[..i] + repl + s[(i + find.Length)..];
    }
}
