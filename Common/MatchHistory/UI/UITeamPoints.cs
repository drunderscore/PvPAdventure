using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MatchHistory.UI;

public sealed class UITeamPoints : UIElement
{
    private readonly TeamPoints[] _scores;

    public UITeamPoints(TeamPoints[] scores)
    {
        _scores = scores ?? [];
        Array.Sort(_scores, static (a, b) =>
        {
            int c = b.Points.CompareTo(a.Points); // higher points first
            return c != 0 ? c : ((int)a.Team).CompareTo((int)b.Team); // stable tie order
        });

        Width.Set(0f, 1f);
        Height.Set(45f, 0f); // room for trophy above
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        if (_scores.Length == 0)
            return;

        CalculatedStyle dim = GetDimensions();

        const int pointWidth = 50;
        const int pointHeight = 24;

        int maxPoints = _scores.Max(s => s.Points);

        float availableWidth = dim.Width;
        float neededWidth = _scores.Length * pointWidth;
        float scale = Math.Min(1f, availableWidth / Math.Max(1f, neededWidth));

        float w = pointWidth * scale;
        float h = pointHeight * scale;

        float totalW = _scores.Length * w;
        float x = dim.X + (dim.Width - totalW) * 0.5f;
        float y = dim.Y + (dim.Height - h) * 0.5f;

        Vector2 textScale = new(scale, scale);

        Texture2D trophyTex = Ass.Icon_Trophy.Value;
        Vector2 trophyOrigin = trophyTex.Size() * 0.5f;
        //float trophyScale = 0.5f;

        for (int i = 0; i < _scores.Length; i++)
        {
            Team team = _scores[i].Team;
            int points = _scores[i].Points;

            Rectangle rect = new((int)(x + i * w), (int)y, (int)w, (int)h);
            Utils.DrawInvBG(sb, rect, Main.teamColor[(int)team] * 0.7f);

            //if (points == maxPoints)
            //{
            //    Vector2 trophyPos = new(rect.Center.X, rect.Top - trophyTex.Height * trophyScale * 0.5f + 3f);
            //    sb.Draw(trophyTex, trophyPos, null, Color.White, 0f, trophyOrigin, trophyScale, SpriteEffects.None, 0f);
            //}

            // Draw team points
            string text = points.ToString();
            Vector2 metrics = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, textScale);
            Vector2 pos = new(rect.X + (rect.Width - metrics.X) * 0.5f, rect.Y + (rect.Height - metrics.Y) * 0.5f + 6.5f);
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text, pos, Color.White, 0f, Vector2.Zero, new Vector2(0.8f));
        }
    }

    public override bool ContainsPoint(Vector2 point) => false;
}

