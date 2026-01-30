using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World.Outlines.TileOutlines;

// System that manages lifecycle for render target for tiles.
[Autoload(Side = ModSide.Client)]
internal sealed class TileOutlineSystem : ModSystem
{
    private readonly Dictionary<Key, TileOutlineRenderTarget> _cache = [];

    public bool TryGet(int tileType, int frameX, int frameY, int tileW, int tileH, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        target = null;
        origin = Vector2.Zero;

        Key key = new(tileType, frameX, frameY, tileW, tileH, border.PackedValue);

        if (!_cache.TryGetValue(key, out TileOutlineRenderTarget rt))
        {
            rt = new TileOutlineRenderTarget();
            _cache[key] = rt;
            Main.ContentThatNeedsRenderTargets.Add(rt);
        }

        rt.UseTile(tileType, frameX, frameY, tileW, tileH, border);

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

    private readonly record struct Key(int TileType, int FrameX, int FrameY, int TileW, int TileH, uint ColorPacked);
}
