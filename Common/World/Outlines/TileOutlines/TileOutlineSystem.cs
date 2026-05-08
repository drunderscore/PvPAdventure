using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace PvPAdventure.Common.World.Outlines.TileOutlines;

// System that manages lifecycle for render target for tiles.
[Autoload(Side = ModSide.Client)]
internal sealed class TileOutlineSystem : ModSystem
{
    private readonly Dictionary<Key, TileOutlineRenderTarget> _cache = [];

    public bool TryGet(int tileType, int frameX, int frameY, int tileW, int tileH, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        const int coordinateWidth = 16;
        const int coordinatePadding = 2;

        int[] frameXs = new int[tileW * tileH];
        int[] frameYs = new int[frameXs.Length];
        int[] heights = new int[tileH];

        for (int y = 0; y < tileH; y++)
        {
            heights[y] = 16;
            for (int x = 0; x < tileW; x++)
            {
                int index = y * tileW + x;
                frameXs[index] = frameX + x * (coordinateWidth + coordinatePadding);
                frameYs[index] = frameY + y * (heights[y] + coordinatePadding);
            }
        }

        return TryGet(tileType, tileW, tileH, coordinateWidth, 0, frameXs, frameYs, heights, border, out target, out origin);
    }

    public bool TryGet(int tileType, Point tileOrigin, int tileW, int tileH, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        TileObjectData data = TileObjectData.GetTileData(Main.tile[tileOrigin.X, tileOrigin.Y]);
        int coordinateWidth = data?.CoordinateWidth ?? 16;
        int[] heights = new int[tileH];
        int[] frameXs = new int[tileW * tileH];
        int[] frameYs = new int[frameXs.Length];

        for (int y = 0; y < tileH; y++)
        {
            heights[y] = data?.CoordinateHeights is { } dataHeights && y < dataHeights.Length ? dataHeights[y] : 16;
            for (int x = 0; x < tileW; x++)
            {
                Tile tile = Main.tile[tileOrigin.X + x, tileOrigin.Y + y];
                int index = y * tileW + x;
                frameXs[index] = tile.TileFrameX;
                frameYs[index] = tile.TileFrameY;
            }
        }

        return TryGet(tileType, tileW, tileH, coordinateWidth, data?.DrawYOffset ?? 0, frameXs, frameYs, heights, border, out target, out origin);
    }

    private bool TryGet(int tileType, int tileW, int tileH, int coordinateWidth, int drawYOffset, int[] frameXs, int[] frameYs, int[] heights, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        target = null;
        origin = Vector2.Zero;

        HashCode hash = new();
        hash.Add(coordinateWidth);
        hash.Add(drawYOffset);
        foreach (int height in heights)
            hash.Add(height);
        foreach (int frame in frameXs)
            hash.Add(frame);
        foreach (int frame in frameYs)
            hash.Add(frame);

        int frameHash = hash.ToHashCode();
        Key key = new(tileType, tileW, tileH, border.PackedValue, frameHash);

        if (!_cache.TryGetValue(key, out TileOutlineRenderTarget rt))
        {
            rt = new TileOutlineRenderTarget();
            _cache[key] = rt;
            Main.ContentThatNeedsRenderTargets.Add(rt);
        }

        rt.UseTile(tileType, tileW, tileH, coordinateWidth, drawYOffset, frameHash, frameXs, frameYs, heights, border);

        target = rt.GetOutlineTarget();
        if (target == null)
            return false;

        origin = new Vector2(target.Width * 0.5f, target.Height * 0.5f);
        return true;
    }

    public override void Unload()
    {
        if (!Main.dedServ)
        {
            foreach (TileOutlineRenderTarget rt in _cache.Values)
                Main.ContentThatNeedsRenderTargets.Remove(rt);
        }

        _cache.Clear();
    }

    private readonly record struct Key(int TileType, int TileW, int TileH, uint ColorPacked, int FrameHash);
}
