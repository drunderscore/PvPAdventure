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
    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Vector2 worldPosition, int fadePixels = 0, int shrinkPadding = 5, Color? overrideColor = null)
    {
        if (!TryGetMapBG(worldPosition, out Texture2D texture, out Color color))
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

        DrawZoomed(sb, texture, rect, overrideColor ?? color, fadePixels);
    }

    private static bool TryGetMapBG(Vector2 worldPosition, out Texture2D texture, out Color color)
    {
        texture = null;
        color = Color.White;

        int tileX = (int)(worldPosition.X / 16f);
        int tileY = (int)(worldPosition.Y / 16f);

        if (!WorldGen.InWorld(tileX, tileY, 10))
            return false;

        Tile tile = Framing.GetTileSafely(tileX, tileY);
        int wall = tile.WallType;
        float tileYf = worldPosition.Y / 16f;

        BiomeSample sample = SampleBiomeTiles(tileX, tileY);
        bool underground = worldPosition.Y > Main.worldSurface * 16.0;
        bool ocean = tileYf < Main.worldSurface + 10.0 && (tileX < 380 || tileX > Main.maxTilesX - 380);
        int bgIndex;

        if (worldPosition.Y > (Main.maxTilesY - 232) * 16f)
        {
            bgIndex = 2;
        }
        else if (wall is WallID.BlueDungeonUnsafe or WallID.GreenDungeonUnsafe or WallID.PinkDungeonUnsafe)
        {
            bgIndex = 4;
        }
        else if (wall == 87)
        {
            bgIndex = 13;
        }
        else if (underground)
        {
            bgIndex = wall switch
            {
                86 or 108 => 15,
                180 or 184 => 16,
                178 or 183 => 17,
                62 or 263 => 18,
                _ when sample.Mushroom => 20,
                _ when sample.Corrupt && sample.Desert => 39,
                _ when sample.Corrupt && sample.Snow => 33,
                _ when sample.Corrupt => 22,
                _ when sample.Crimson && sample.Desert => 40,
                _ when sample.Crimson && sample.Snow => 34,
                _ when sample.Crimson => 23,
                _ when sample.Hallow && sample.Desert => 41,
                _ when sample.Hallow && sample.Snow => 35,
                _ when sample.Hallow => 21,
                _ when sample.Snow => 3,
                _ when sample.Jungle => 12,
                _ when sample.Desert => 14,
                _ => tileYf > Main.rockLayer ? 31 : 1
            };
        }
        else if (sample.Mushroom)
        {
            bgIndex = 19;
        }
        else if (tileYf < Main.worldSurface * 0.45f)
        {
            bgIndex = 32;
        }
        else if (sample.Corrupt)
        {
            bgIndex = sample.Desert ? 36 : 5;
        }
        else if (sample.Crimson)
        {
            bgIndex = sample.Desert ? 37 : 6;
        }
        else if (sample.Hallow)
        {
            bgIndex = sample.Desert ? 38 : 7;
        }
        else if (ocean)
        {
            bgIndex = 10;
        }
        else if (sample.Snow)
        {
            bgIndex = 11;
        }
        else if (sample.Jungle)
        {
            bgIndex = 8;
        }
        else if (sample.Desert)
        {
            bgIndex = 9;
        }
        else if (Main.bloodMoon)
        {
            bgIndex = 25;
            color *= 2f;
        }
        else if (sample.Graveyard)
        {
            bgIndex = 26;
        }
        else
        {
            bgIndex = 0;
        }

        int safeIndex = bgIndex >= 0 && bgIndex < Ass.MapBG.Length ? bgIndex : 0;
        texture = Ass.MapBG[safeIndex]?.Value;
        return texture != null;
    }

    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, int mapBgIndex, int fadePixels = 0, int shrinkPadding = 5, Color? overrideColor = null)
    {
        int safeIndex = mapBgIndex >= 0 && mapBgIndex < Ass.MapBG.Length ? mapBgIndex : 0;
        Texture2D texture = Ass.MapBG[safeIndex]?.Value;

        if (texture == null)
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

        DrawZoomed(sb, texture, rect, overrideColor ?? Color.White, fadePixels);
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

    private readonly struct BiomeSample
    {
        public readonly bool Snow;
        public readonly bool Desert;
        public readonly bool Jungle;
        public readonly bool Corrupt;
        public readonly bool Crimson;
        public readonly bool Hallow;
        public readonly bool Mushroom;
        public readonly bool Graveyard;

        public BiomeSample(bool snow, bool desert, bool jungle, bool corrupt, bool crimson, bool hallow, bool mushroom, bool graveyard)
        {
            Snow = snow;
            Desert = desert;
            Jungle = jungle;
            Corrupt = corrupt;
            Crimson = crimson;
            Hallow = hallow;
            Mushroom = mushroom;
            Graveyard = graveyard;
        }
    }

    private static BiomeSample SampleBiomeTiles(int tileX, int tileY)
    {
        bool snow = false;
        bool desert = false;
        bool jungle = false;
        bool corrupt = false;
        bool crimson = false;
        bool hallow = false;
        bool mushroom = false;
        bool graveyard = false;

        for (int y = -8; y <= 8; y++)
        {
            for (int x = -8; x <= 8; x++)
            {
                int sampleX = tileX + x;
                int sampleY = tileY + y;

                if (!WorldGen.InWorld(sampleX, sampleY, 1))
                    continue;

                Tile tile = Framing.GetTileSafely(sampleX, sampleY);

                if (tile.HasTile)
                {
                    int type = tile.TileType;

                    snow |= IsSnowTile(type);
                    desert |= IsDesertTile(type);
                    jungle |= IsJungleTile(type);
                    corrupt |= IsCorruptTile(type);
                    crimson |= IsCrimsonTile(type);
                    hallow |= IsHallowTile(type);
                    mushroom |= type is TileID.MushroomGrass or TileID.MushroomPlants;
                    graveyard |= type == TileID.Tombstones;
                }

                //graveyard |= tile.WallType == WallID.GraveyardEcho;
            }
        }

        return new BiomeSample(snow, desert, jungle, corrupt, crimson, hallow, mushroom, graveyard);
    }

    private static bool IsSnowTile(int tileType) =>
        tileType is TileID.SnowBlock or TileID.IceBlock or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce;

    private static bool IsDesertTile(int tileType) =>
        tileType is TileID.Sand or TileID.HardenedSand or TileID.Sandstone or TileID.Ebonsand or TileID.Crimsand or TileID.Pearlsand;

    private static bool IsJungleTile(int tileType) =>
        tileType is TileID.JungleGrass or TileID.Mud or TileID.JunglePlants or TileID.JungleThorns or TileID.Hive;

    private static bool IsCorruptTile(int tileType) =>
        tileType is TileID.CorruptGrass or TileID.Ebonstone or TileID.Ebonsand or TileID.CorruptJungleGrass;

    private static bool IsCrimsonTile(int tileType) =>
        tileType is TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimsand or TileID.CrimsonJungleGrass;

    private static bool IsHallowTile(int tileType) =>
        tileType is TileID.HallowedGrass or TileID.Pearlstone or TileID.Pearlsand;


}
