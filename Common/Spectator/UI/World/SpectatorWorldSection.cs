using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.World;

internal sealed class SpectatorWorldSection : UIPanel
{
    private readonly string title;
    private readonly Action<SpectatorWorldSection, SpriteBatch, Rectangle> drawContent;

    public SpectatorWorldSection(string title, float height, Action<SpectatorWorldSection, SpriteBatch, Rectangle> drawContent)
    {
        this.title = title;
        this.drawContent = drawContent;

        Width.Set(0f, 1f);
        Height.Set(height, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = new Color(116, 154, 255) * 0.75f;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        Rectangle box = GetDimensions().ToRectangle();
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
        Utils.DrawBorderString(sb, title, new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);
        drawContent?.Invoke(this, sb, box);
    }
}
