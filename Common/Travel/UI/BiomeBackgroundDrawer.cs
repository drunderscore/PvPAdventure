using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace PvPAdventure.Common.Travel.UI;

internal class BiomeBackgroundDrawer
{
    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Vector2 worldPosition, int fadePixels = 0, int shrinkPadding = 5, Player zonePlayer = null)
    {
        if (!TryGetMapBG(worldPosition, zonePlayer, out Texture2D texture, out Color color))
            return;

        if (shrinkPadding > 0)
        {
            rect.X += shrinkPadding;
            rect.Y += shrinkPadding;
            rect.Width -= shrinkPadding * 2;
            rect.Height -= shrinkPadding * 2;
        }

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        DrawZoomed(sb, texture, rect, color, fadePixels);
    }

    private static bool TryGetMapBG(Vector2 worldPosition, Player zonePlayer, out Texture2D texture, out Color color)
    {
        texture = null;
        color = Color.White;

        int tileX = (int)(worldPosition.X / 16f);
        int tileY = (int)(worldPosition.Y / 16f);

        if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
            return false;

        Tile tile = Main.tile[tileX, tileY];

        if (tile == null)
            return false;

        int wall = tile.WallType;
        int bgIndex = -1;

        float worldY = worldPosition.Y;
        float tileYFloat = worldY / 16f;
        bool useZones = zonePlayer?.active == true;

        if (worldY > (Main.maxTilesY - 232) * 16f)
        {
            bgIndex = 2;
        }
        else if (useZones && zonePlayer.ZoneDungeon || wall == WallID.BlueDungeonUnsafe || wall == WallID.GreenDungeonUnsafe || wall == WallID.PinkDungeonUnsafe)
        {
            bgIndex = 4;
        }
        else if (wall == 87)
        {
            bgIndex = 13;
        }
        else if (worldY > Main.worldSurface * 16.0)
        {
            bgIndex = wall switch
            {
                86 or 108 => 15,
                180 or 184 => 16,
                178 or 183 => 17,
                62 or 263 => 18,
                _ when useZones && zonePlayer.ZoneGlowshroom => 20,
                _ when useZones && zonePlayer.ZoneCorrupt && zonePlayer.ZoneDesert => 39,
                _ when useZones && zonePlayer.ZoneCorrupt && zonePlayer.ZoneSnow => 33,
                _ when useZones && zonePlayer.ZoneCorrupt => 22,
                _ when useZones && zonePlayer.ZoneCrimson && zonePlayer.ZoneDesert => 40,
                _ when useZones && zonePlayer.ZoneCrimson && zonePlayer.ZoneSnow => 34,
                _ when useZones && zonePlayer.ZoneCrimson => 23,
                _ when useZones && zonePlayer.ZoneHallow && zonePlayer.ZoneDesert => 41,
                _ when useZones && zonePlayer.ZoneHallow && zonePlayer.ZoneSnow => 35,
                _ when useZones && zonePlayer.ZoneHallow => 21,
                _ when useZones && zonePlayer.ZoneSnow => 3,
                _ when useZones && zonePlayer.ZoneJungle => 12,
                _ when useZones && zonePlayer.ZoneDesert => 14,
                _ when useZones && zonePlayer.ZoneRockLayerHeight => 31,
                _ => tileYFloat > Main.rockLayer ? 31 : 1
            };
        }
        else if (useZones && zonePlayer.ZoneGlowshroom)
        {
            bgIndex = 19;
        }
        else
        {
            if (useZones && zonePlayer.dead)
                color = new Color(50, 50, 50, 255);

            if (useZones && zonePlayer.ZoneSkyHeight)
                bgIndex = 32;
            else if (useZones && zonePlayer.ZoneCorrupt)
                bgIndex = zonePlayer.ZoneDesert ? 36 : 5;
            else if (useZones && zonePlayer.ZoneCrimson)
                bgIndex = zonePlayer.ZoneDesert ? 37 : 6;
            else if (useZones && zonePlayer.ZoneHallow)
                bgIndex = zonePlayer.ZoneDesert ? 38 : 7;
            else if (tileYFloat < Main.worldSurface + 10.0 && (tileX < 380 || tileX > Main.maxTilesX - 380))
                bgIndex = 10;
            else if (useZones && zonePlayer.ZoneSnow)
                bgIndex = 11;
            else if (useZones && zonePlayer.ZoneJungle)
                bgIndex = 8;
            else if (useZones && zonePlayer.ZoneDesert)
                bgIndex = 9;
            else if (Main.bloodMoon)
            {
                bgIndex = 25;
                color *= 2f;
            }
            else if (useZones && zonePlayer.ZoneGraveyard)
                bgIndex = 26;
            else
                bgIndex = 0;
        }

        int safeIndex = bgIndex >= 0 && bgIndex < Ass.MapBG.Length ? bgIndex : 0;
        texture = Ass.MapBG[safeIndex]?.Value;

        return texture != null;
    }

    public static void DrawFadedFill(SpriteBatch sb, Rectangle rect, Color color, int fadePixels = 0)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        if (fadePixels <= 0)
        {
            sb.Draw(pixel, rect, color);
            return;
        }

        for (int y = 0; y < rect.Height; y++)
        {
            for (int x = 0; x < rect.Width; x++)
            {
                float alpha = GetEdgeFadeAlpha(x, y, rect.Width, rect.Height, fadePixels);

                if (alpha > 0f)
                    sb.Draw(pixel, new Rectangle(rect.X + x, rect.Y + y, 1, 1), color * alpha);
            }
        }
    }

    private static void DrawZoomed(SpriteBatch sb, Texture2D tex, Rectangle dest, Color color, int fadePixels)
    {
        float scale = MathHelper.Max(dest.Width / (float)tex.Width, dest.Height / (float)tex.Height);
        int srcW = Math.Max(1, (int)(dest.Width / scale));
        int srcH = Math.Max(1, (int)(dest.Height / scale));
        Rectangle src = new((tex.Width - srcW) / 2, (tex.Height - srcH) / 2, srcW, srcH);

        if (fadePixels <= 0)
        {
            sb.Draw(tex, dest, src, color);
            return;
        }

        for (int y = 0; y < dest.Height; y++)
        {
            int srcY = src.Y + src.Height * y / dest.Height;

            for (int x = 0; x < dest.Width; x++)
            {
                float alpha = GetEdgeFadeAlpha(x, y, dest.Width, dest.Height, fadePixels);

                if (alpha <= 0f)
                    continue;

                int srcX = src.X + src.Width * x / dest.Width;
                sb.Draw(tex, new Rectangle(dest.X + x, dest.Y + y, 1, 1), new Rectangle(srcX, srcY, 1, 1), color * alpha);
            }
        }
    }

    private static float GetEdgeFadeAlpha(int x, int y, int width, int height, int fadePixels)
    {
        if (fadePixels <= 0)
            return 1f;

        int left = x;
        int right = width - 1 - x;
        int top = y;
        int bottom = height - 1 - y;

        int distance = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
        float progress = MathHelper.Clamp(distance / (float)Math.Max(1, fadePixels), 0f, 1f);

        return GetAggressiveFade(progress);
    }

    private static float GetAggressiveFade(float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);

        if (progress < 0.2f)
            return MathHelper.SmoothStep(0f, 0.01f, progress / 0.2f);

        if (progress < 0.4f)
            return MathHelper.SmoothStep(0.01f, 0.1f, (progress - 0.2f) / 0.2f);

        if (progress < 0.6f)
            return MathHelper.SmoothStep(0.1f, 0.5f, (progress - 0.4f) / 0.2f);

        if (progress < 0.8f)
            return MathHelper.SmoothStep(0.5f, 0.9f, (progress - 0.6f) / 0.2f);

        return MathHelper.SmoothStep(0.9f, 1f, (progress - 0.8f) / 0.2f);
    }

    /// <summary>
    /// Draws the MapBG "biome" texture the player is currently in.
    /// </summary>
    //public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Player player, int topFadePixels = 0)
    //{ 
    //    if (player == null || !player.active)
    //        return;

    //    // Player tile coordinates
    //    int tileX = (int)(player.Center.X / 16f);
    //    int tileY = (int)(player.Center.Y / 16f);

    //    Tile tile = Main.tile[tileX, tileY];
    //    if (tile == null)
    //        return;

    //    int wall = tile.WallType;
    //    int bgIndex = -1;
    //    Color color = Color.White;

    //    // Use player Y position to determine underground/cavern/hell layers
    //    float playerYWorld = player.Center.Y;
    //    float playerYTiles = playerYWorld / 16f;

    //    // Hell layer
    //    if (playerYWorld > (Main.maxTilesY - 232) * 16)
    //    {
    //        bgIndex = 2;
    //    }
    //    // Dungeon
    //    else if (player.ZoneDungeon)
    //    {
    //        bgIndex = 4;
    //    }
    //    // Spider cave (?) wall
    //    else if (wall == 87)
    //    {
    //        bgIndex = 13;
    //    }
    //    // Underground / cavern backgrounds
    //    else if (playerYWorld > Main.worldSurface * 16.0)
    //    {
    //        bgIndex = wall switch
    //        {
    //            86 or 108 => 15,
    //            180 or 184 => 16,
    //            178 or 183 => 17,
    //            62 or 263 => 18,
    //            _ => player.ZoneGlowshroom ? 20 :
    //                 player.ZoneCorrupt ? player.ZoneDesert ? 39 : player.ZoneSnow ? 33 : 22 :
    //                 player.ZoneCrimson ? player.ZoneDesert ? 40 : player.ZoneSnow ? 34 : 23 :
    //                 player.ZoneHallow ? player.ZoneDesert ? 41 : player.ZoneSnow ? 35 : 21 :
    //                 player.ZoneSnow ? 3 :
    //                 player.ZoneJungle ? 12 :
    //                 player.ZoneDesert ? 14 :
    //                 player.ZoneRockLayerHeight ? 31 : 1
    //        };
    //    }
    //    // Surface mushroom biome
    //    else if (player.ZoneGlowshroom)
    //    {
    //        bgIndex = 19;
    //    }
    //    else
    //    {
    //        color = Color.White;

    //        if (player.dead)
    //            color = new Color(50, 50, 50, 255);

    //        int midTileX = tileX;

    //        if (player.ZoneSkyHeight)
    //            bgIndex = 32;
    //        else if (player.ZoneCorrupt)
    //            bgIndex = player.ZoneDesert ? 36 : 5;
    //        else if (player.ZoneCrimson)
    //            bgIndex = player.ZoneDesert ? 37 : 6;
    //        else if (player.ZoneHallow)
    //            bgIndex = player.ZoneDesert ? 38 : 7;

    //        // "Ocean" style edges
    //        else if (playerYTiles < Main.worldSurface + 10.0 &&
    //                 (midTileX < 380 || midTileX > Main.maxTilesX - 380))
    //            bgIndex = 10;
    //        else if (player.ZoneSnow)
    //            bgIndex = 11;
    //        else if (player.ZoneJungle)
    //            bgIndex = 8;
    //        else if (player.ZoneDesert)
    //            bgIndex = 9;
    //        else if (Main.bloodMoon)
    //        {
    //            bgIndex = 25;
    //            color *= 2f;
    //        }
    //        else if (player.ZoneGraveyard)
    //            bgIndex = 26;
    //    }

    //    int safeIndex = bgIndex >= 0 && bgIndex < Ass.MapBG.Length ? bgIndex : 0;
    //    var asset = Ass.MapBG[safeIndex];

    //    int shrinkPadding = 5;
    //    rect.X += shrinkPadding;
    //    rect.Y += shrinkPadding;
    //    rect.Width -= shrinkPadding * 2;
    //    rect.Height -= shrinkPadding * 2;

    //    if (asset == null || asset.Value == null)
    //        return;

    //    //sb.Draw(asset.Value, rect, color);

    //    //Texture2D tex = asset.Value;

    //    //float scaleX = (float)rect.Width / tex.Width;
    //    //float scaleY = (float)rect.Height / tex.Height;
    //    //float scale = MathHelper.Max(scaleX, scaleY); // Max = fill, Min = fit

    //    //int srcW = (int)(rect.Width / scale);
    //    //int srcH = (int)(rect.Height / scale);
    //    //int srcX = (tex.Width - srcW) / 2;
    //    //int srcY = (tex.Height - srcH) / 2;

    //    //Rectangle srcRect = new(srcX, srcY, srcW, srcH);
    //    //sb.Draw(tex, rect, srcRect, color);

    //    DrawZoomed(sb, asset.Value, rect, color, topFadePixels);
    //}
}
