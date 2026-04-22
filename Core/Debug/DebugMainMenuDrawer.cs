using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal static class DebugMainMenuDrawer
{
    private static readonly Color SizeTextColor = Color.Gray * 0.55f;

    internal static void DrawSkinUICard(SpriteBatch sb, Rectangle cardRect, Rectangle textRect, Rectangle itemSlotRect, Rectangle gemRect)
    {
        //DrawRect(sb, cardRect);
        DrawRect(sb, textRect);
        DrawRect(sb, itemSlotRect);
        DrawRect(sb, gemRect);
    }

    private static void DrawRect(SpriteBatch sb, Rectangle rect)
    {
        DrawRectangle(sb, rect, Color.Red * 0.3f);
        Color textColor = Color.LightGray * 0.6f;
        float textScale = 0.6f;
        Utils.DrawBorderString(sb, $"{rect.Width}x{rect.Height}", new Vector2(rect.X + 2f, rect.Y + 2f), textColor, textScale);
    }

    private static void DrawRectangle(SpriteBatch sb, Rectangle rect, Color color)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        sb.Draw(pixel, rect, color * 0.3f);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), Color.Black);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), Color.Black);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), Color.Black);
        sb.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), Color.Black);
    }
}
#endif