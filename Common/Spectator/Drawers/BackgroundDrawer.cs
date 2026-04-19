using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.Spectator.Drawers;

internal static class BackgroundDrawer
{
    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Player player, bool listMode)
    {
        DrawMapFullscreenBackground(sb, rect, BiomeHelper.GetBiomeVisual(player), listMode);
    }

    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, NPC npc, bool listMode)
    {
        DrawMapFullscreenBackground(sb, rect, BiomeHelper.GetBiomeVisual(npc), listMode);
    }

    private static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, BiomeHelper.PlayerBiomeVisual biome, bool listMode)
    {
        Texture2D tex;
        Color color = biome.BackgroundColor;

        if (biome.BackgroundIndex < 0)
        {
            tex = Ass.BG_Shimmer.Value;
            color = new Color(180, 140, 255);
        }
        else
        {
            int safeIndex = Math.Clamp(biome.BackgroundIndex, 0, Ass.MapBG.Length - 1);
            //Asset<Texture2D> asset = Ass.MapBG[safeIndex];
            Asset<Texture2D> asset = TextureAssets.MapBGs[safeIndex];
            if (asset?.Value == null)
                return;

            tex = asset.Value;
        }

        int padding = 0;
        rect.X += padding;
        rect.Y += padding;
        rect.Width -= padding * 2;
        rect.Height -= padding * 2;

        if (listMode)
        {
            // If you want perfect aspect ratio, keep these 3 lines.
            //int drawWidth = (int)(rect.Height * (tex.Width / (float)tex.Height));
            //Rectangle drawRect = new(rect.X, rect.Y, drawWidth, rect.Height);
            //DrawFade(sb, tex, drawRect, tex.Bounds, color, fadeLeft: 0.02f, fadeRight: 1f, fadeTop: 0.02f, fadeBottom: 0.02f);

            float widthScale = 0.8f; // or 0.8f
            int drawWidth = (int)(rect.Width * widthScale);

            Rectangle drawRect = new(
                rect.X,
                rect.Y,
                drawWidth,
                rect.Height);

            DrawFade(sb, tex, drawRect, tex.Bounds, color, fadeLeft: 0.02f, fadeRight: 1f, fadeTop: 0.02f, fadeBottom: 0.02f);

            // If you want stretched out, keep this line.
            //DrawFade(sb, tex, drawRect, tex.Bounds, color, fadeLeft: 0.02f, fadeRight: 1f, fadeTop: 0.02f, fadeBottom: 0.02f);
            return;
        }

        float sourceWidth = tex.Height == 0 ? tex.Width : rect.Width * (tex.Height / (float)rect.Height);
        int croppedWidth = Math.Min(tex.Width, (int)Math.Round(sourceWidth));
        int sourceX = (tex.Width - croppedWidth) / 2;
        Rectangle source = new(sourceX, 0, croppedWidth, tex.Height);

        DrawFade(sb, tex, rect, tex.Bounds, color, fadeLeft: 0.1f, fadeRight: 0.9f, fadeTop: 0.05f, fadeBottom: 0.05f);
    }

    #region Draw helpers
    /// <summary>
    /// Draws an image that fades opacity slowly from all 4 sides with normalized 0..1 portions.
    /// </summary>
    public static void DrawFade(SpriteBatch sb, Texture2D texture, Rectangle target, Rectangle source, Color color, float fadeLeft = 0f, float fadeRight = 0f, float fadeTop = 0f, float fadeBottom = 0f, int sliceWidth = 1, int sliceHeight = 1)
    {
        if (texture == null || target.Width <= 0 || target.Height <= 0 || source.Width <= 0 || source.Height <= 0)
            return;

        fadeLeft = MathHelper.Clamp(fadeLeft, 0f, 1f);
        fadeRight = MathHelper.Clamp(fadeRight, 0f, 1f);
        fadeTop = MathHelper.Clamp(fadeTop, 0f, 1f);
        fadeBottom = MathHelper.Clamp(fadeBottom, 0f, 1f);

        sliceWidth = Math.Max(1, sliceWidth);
        sliceHeight = Math.Max(1, sliceHeight);

        int leftFadeWidth = (int)(target.Width * fadeLeft);
        int rightFadeWidth = (int)(target.Width * fadeRight);
        int topFadeHeight = (int)(target.Height * fadeTop);
        int bottomFadeHeight = (int)(target.Height * fadeBottom);

        for (int y = 0; y < target.Height; y += sliceHeight)
        {
            int currentSliceHeight = Math.Min(sliceHeight, target.Height - y);

            for (int x = 0; x < target.Width; x += sliceWidth)
            {
                int currentSliceWidth = Math.Min(sliceWidth, target.Width - x);

                Rectangle dest = new(target.X + x, target.Y + y, currentSliceWidth, currentSliceHeight);

                int srcX = source.X + (int)(x / (float)target.Width * source.Width);
                int srcY = source.Y + (int)(y / (float)target.Height * source.Height);
                int srcWidth = Math.Max(1, (int)(currentSliceWidth / (float)target.Width * source.Width));
                int srcHeight = Math.Max(1, (int)(currentSliceHeight / (float)target.Height * source.Height));

                if (srcX + srcWidth > source.Right)
                    srcWidth = source.Right - srcX;
                if (srcY + srcHeight > source.Bottom)
                    srcHeight = source.Bottom - srcY;

                if (srcWidth <= 0 || srcHeight <= 0)
                    continue;

                Rectangle src = new(srcX, srcY, srcWidth, srcHeight);

                float alphaX = 1f;
                float alphaY = 1f;

                if (leftFadeWidth > 0 && x < leftFadeWidth)
                    alphaX = Math.Min(alphaX, x / (float)leftFadeWidth);

                if (rightFadeWidth > 0 && x > target.Width - rightFadeWidth)
                    alphaX = Math.Min(alphaX, (target.Width - x) / (float)rightFadeWidth);

                if (topFadeHeight > 0 && y < topFadeHeight)
                    alphaY = Math.Min(alphaY, y / (float)topFadeHeight);

                if (bottomFadeHeight > 0 && y > target.Height - bottomFadeHeight)
                    alphaY = Math.Min(alphaY, (target.Height - y) / (float)bottomFadeHeight);

                float alpha = MathHelper.Clamp(alphaX * alphaY, 0f, 1f);
                sb.Draw(texture, dest, src, color * alpha);
            }
        }
    }

    #endregion
}
