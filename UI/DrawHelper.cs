using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPAdventure.UI;

public static class DrawHelper
{
    public static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle dest, int left, int top, int right, int bottom, Color color = default)
    {
        if (color == default)
            color = Color.White;

        if (texture == null || dest.Width <= 0 || dest.Height <= 0)
            return;

        int texWidth = texture.Width;
        int texHeight = texture.Height;

        left = Math.Clamp(left, 0, texWidth);
        right = Math.Clamp(right, 0, texWidth - left);
        top = Math.Clamp(top, 0, texHeight);
        bottom = Math.Clamp(bottom, 0, texHeight - top);

        int centerSourceWidth = Math.Max(0, texWidth - left - right);
        int centerSourceHeight = Math.Max(0, texHeight - top - bottom);

        int drawLeft = Math.Min(left, dest.Width);
        int drawRight = Math.Min(right, Math.Max(0, dest.Width - drawLeft));
        int drawTop = Math.Min(top, dest.Height);
        int drawBottom = Math.Min(bottom, Math.Max(0, dest.Height - drawTop));

        int centerDestWidth = Math.Max(0, dest.Width - drawLeft - drawRight);
        int centerDestHeight = Math.Max(0, dest.Height - drawTop - drawBottom);

        Rectangle srcTopLeft = new(0, 0, left, top);
        Rectangle srcTop = new(left, 0, centerSourceWidth, top);
        Rectangle srcTopRight = new(texWidth - right, 0, right, top);

        Rectangle srcLeft = new(0, top, left, centerSourceHeight);
        Rectangle srcCenter = new(left, top, centerSourceWidth, centerSourceHeight);
        Rectangle srcRight = new(texWidth - right, top, right, centerSourceHeight);

        Rectangle srcBottomLeft = new(0, texHeight - bottom, left, bottom);
        Rectangle srcBottom = new(left, texHeight - bottom, centerSourceWidth, bottom);
        Rectangle srcBottomRight = new(texWidth - right, texHeight - bottom, right, bottom);

        Rectangle dstTopLeft = new(dest.X, dest.Y, drawLeft, drawTop);
        Rectangle dstTop = new(dest.X + drawLeft, dest.Y, centerDestWidth, drawTop);
        Rectangle dstTopRight = new(dest.Right - drawRight, dest.Y, drawRight, drawTop);

        Rectangle dstLeft = new(dest.X, dest.Y + drawTop, drawLeft, centerDestHeight);
        Rectangle dstCenter = new(dest.X + drawLeft, dest.Y + drawTop, centerDestWidth, centerDestHeight);
        Rectangle dstRight = new(dest.Right - drawRight, dest.Y + drawTop, drawRight, centerDestHeight);

        Rectangle dstBottomLeft = new(dest.X, dest.Bottom - drawBottom, drawLeft, drawBottom);
        Rectangle dstBottom = new(dest.X + drawLeft, dest.Bottom - drawBottom, centerDestWidth, drawBottom);
        Rectangle dstBottomRight = new(dest.Right - drawRight, dest.Bottom - drawBottom, drawRight, drawBottom);

        if (srcTopLeft.Width > 0 && srcTopLeft.Height > 0 && dstTopLeft.Width > 0 && dstTopLeft.Height > 0)
            sb.Draw(texture, dstTopLeft, srcTopLeft, color);
        if (srcTop.Width > 0 && srcTop.Height > 0 && dstTop.Width > 0 && dstTop.Height > 0)
            sb.Draw(texture, dstTop, srcTop, color);
        if (srcTopRight.Width > 0 && srcTopRight.Height > 0 && dstTopRight.Width > 0 && dstTopRight.Height > 0)
            sb.Draw(texture, dstTopRight, srcTopRight, color);

        if (srcLeft.Width > 0 && srcLeft.Height > 0 && dstLeft.Width > 0 && dstLeft.Height > 0)
            sb.Draw(texture, dstLeft, srcLeft, color);
        if (srcCenter.Width > 0 && srcCenter.Height > 0 && dstCenter.Width > 0 && dstCenter.Height > 0)
            sb.Draw(texture, dstCenter, srcCenter, color);
        if (srcRight.Width > 0 && srcRight.Height > 0 && dstRight.Width > 0 && dstRight.Height > 0)
            sb.Draw(texture, dstRight, srcRight, color);

        if (srcBottomLeft.Width > 0 && srcBottomLeft.Height > 0 && dstBottomLeft.Width > 0 && dstBottomLeft.Height > 0)
            sb.Draw(texture, dstBottomLeft, srcBottomLeft, color);
        if (srcBottom.Width > 0 && srcBottom.Height > 0 && dstBottom.Width > 0 && dstBottom.Height > 0)
            sb.Draw(texture, dstBottom, srcBottom, color);
        if (srcBottomRight.Width > 0 && srcBottomRight.Height > 0 && dstBottomRight.Width > 0 && dstBottomRight.Height > 0)
            sb.Draw(texture, dstBottomRight, srcBottomRight, color);
    }
}
