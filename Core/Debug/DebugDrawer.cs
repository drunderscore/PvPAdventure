using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace PvPAdventure.UI;

public static class DebugDrawer
{
    /// <summary>
    /// Draws debug bounds for a UIElement.
    /// </summary>
    public static void DrawElement(SpriteBatch sb, UIElement element, bool drawSize = true)
    {
        if (sb is null || element is null)
            return;

        Color[] colors =
        [
            Color.Red * 0.30f,
            Color.Lime * 0.30f,
            Color.Blue * 0.30f,
            Color.Yellow * 0.30f,
            Color.Cyan * 0.30f,
            Color.Magenta * 0.30f,
            Color.Orange * 0.30f
        ];

        Color color = colors[Math.Abs(element.GetType().Name.GetHashCode()) % colors.Length];

        Rectangle box = element.GetDimensions().ToRectangle();
        sb.Draw(TextureAssets.MagicPixel.Value, box, color);

        if (drawSize)
            Utils.DrawBorderString(sb, $"{element.GetType().Name}\n{box.Width}x{box.Height}", box.TopLeft(), Color.White, 0.7f);
    }

    /// <summary>
    /// Draws debug bounds for a Rectangle.
    /// </summary>
    public static void DrawRectangle(SpriteBatch sb, Rectangle rect, bool drawSize = true)
    {
        if (sb is null)
            return;

        Color[] colors =
        [
            Color.Red * 0.30f,
            Color.Lime * 0.30f,
            Color.Blue * 0.30f,
            Color.Yellow * 0.30f,
            Color.Cyan * 0.30f,
            Color.Magenta * 0.30f,
            Color.Orange * 0.30f
        ];

        Color color = colors[Math.Abs(rect.GetType().Name.GetHashCode()) % colors.Length];

        sb.Draw(TextureAssets.MagicPixel.Value, rect, color);

        if (drawSize)
            Utils.DrawBorderString(sb, $"{rect.GetType().Name}\n{rect.Width}x{rect.Height}", rect.TopLeft(), Color.White, 0.7f);
    }
}