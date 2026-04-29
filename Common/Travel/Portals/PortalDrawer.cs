using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.Travel.Portals;

public static class PortalDrawer
{
    public static void DrawPortalHealthBar(SpriteBatch sb, Vector2 worldPos, int health, int maxHealth, float scale, float alpha)
    {
        if (maxHealth <= 0)
            return;

        float healthRatio = (float)health / maxHealth;
        if (healthRatio > 1f)
            healthRatio = 1f;

        int barPixels = (int)(36f * healthRatio);
        if (barPixels < 3)
            barPixels = 3;

        healthRatio -= 0.1f;
        float green = healthRatio > 0.5f ? 255f : 255f * healthRatio * 2f;
        float red = healthRatio > 0.5f ? 255f * (1f - healthRatio) * 2f : 255f;
        float colorScale = alpha * 0.95f;

        red = MathHelper.Clamp(red * colorScale, 0f, 255f);
        green = MathHelper.Clamp(green * colorScale, 0f, 255f);
        float alphaByte = MathHelper.Clamp(255f * colorScale, 0f, 255f);
        Color barColor = new((byte)red, (byte)green, 0, (byte)alphaByte);

        Vector2 screenOrigin = new(worldPos.X - 18f * scale - Main.screenPosition.X, worldPos.Y - Main.screenPosition.Y);
        Texture2D backTex = TextureAssets.Hb2.Value;
        Texture2D fillTex = TextureAssets.Hb1.Value;

        if (barPixels < 34)
        {
            if (barPixels < 36)
                sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(2, 0, 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels < 34)
                sb.Draw(backTex, screenOrigin + new Vector2((barPixels + 2) * scale, 0f), new Rectangle(barPixels + 2, 0, 36 - barPixels - 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels > 2)
                sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels - 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            sb.Draw(fillTex, screenOrigin + new Vector2((barPixels - 2) * scale, 0f), new Rectangle(32, 0, 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return;
        }

        if (barPixels < 36)
            sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public static void SpawnPortalDust(Vector2 worldPos, float progress = 1f, int dustMultiplier = 1)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);

        for (int i = 0; i < dustMultiplier; i++)
        {
            PotionOfReturnGateHelper gate = new(
                PotionOfReturnGateHelper.GateType.EntryPoint,
                worldPos,
                progress
            );

            gate.SpawnReturnPortalDust();
        }
    }

    public static void DrawHoverIcon(SpriteBatch sb, Player player, Vector2 worldPos, Color outlineColor, float alpha)
    {
        const float pulseSpeed = 6f; // Lower = slower pulse.
        const float pulseScaleVariance = 0.15f; // Higher = larger size change.

        // Draw animated portal. Keep this commented out for now.
        //Texture2D iconTexture = GetPortalAsset(player).Value;
        //Rectangle source = GetPortalFrameRectangle(iconTexture);

        Texture2D iconTexture = PortalAssets.GetPortalMinimapTexture(player.team);
        Rectangle source = iconTexture.Bounds;

        //Vector2 origin = new(source.Width * 0.5f, source.Height * 0.5f); // center
        Vector2 origin = Vector2.Zero;
        Vector2 drawPos = new(Main.mouseX, Main.mouseY);
        Vector2 extraOffset = new(22f, -10);
        drawPos += extraOffset;

        float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseSpeed);
        float iconScale = 1f + pulse * pulseScaleVariance;
        Color drawColor = Color.White * alpha;

        //DrawTextureOutline(sb, iconTexture, drawPos, source, origin, Color.Black * alpha, iconScale, 2f);
        //DrawTextureOutline(sb, iconTexture, drawPos, source, origin, outlineColor * alpha, iconScale, 1f);
        sb.Draw(iconTexture, drawPos, source, drawColor, 0f, origin, iconScale, SpriteEffects.None, 0f);
    }

    //public static void DrawPortalPreview(SpriteBatch sb, Player player, Vector2 position, float scale, bool outline = true, Color drawColor = default, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
//    {
//        Texture2D texture = GetPortalAsset(player).Value;
//        Rectangle source = GetPortalFrameRectangle(texture);
//        Vector2 origin = source.Size() * 0.5f;
//        Color borderColor = GetPortalColor(player);

    //        if (drawColor == default)
    //            drawColor = Color.White;

    //        DrawPortal(sb, texture, position, source, origin, scale, drawColor, borderColor, outline, blackOutlineDistance, colorOutlineDistance);
    //    }

    //    public static void DrawPortal(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, float scale, Color drawColor, Color borderColor, bool outline, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
    //    {
    //        if (outline)
    //        {
    //            float alphaScale = 0.9f * drawColor.A / 255f;
    //            //DrawTextureOutline(sb, texture, position, source, origin, Color.Yellow * alphaScale, scale, blackOutlineDistance);
    //            DrawTextureOutline(sb, texture, position, source, origin, borderColor * alphaScale, scale, colorOutlineDistance);
    //        }

    //        sb.Draw(texture, position, source, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
    //    }
    //private static void DrawTextureOutline(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, Color color, float scale, float distance)
    //    {
    //        Vector2[] offsets =
    //        [
    //            new Vector2(-distance, 0f),
    //            new Vector2(distance, 0f),
    //            new Vector2(0f, -distance),
    //            new Vector2(0f, distance),
    //            new Vector2(-distance, -distance),
    //            new Vector2(-distance, distance),
    //            new Vector2(distance, -distance),
    //            new Vector2(distance, distance)
    //        ];

    //        for (int i = 0; i < offsets.Length; i++)
    //            sb.Draw(texture, position + offsets[i], source, color, 0f, origin, scale, SpriteEffects.None, 0f);
    //    }
}
