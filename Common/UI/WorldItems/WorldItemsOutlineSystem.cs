using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI.WorldItems;

[Autoload(Side = ModSide.Client)]
internal sealed class WorldItemOutlineRenderTargetSystem : ModSystem
{
    // Store item outlines by key
    private readonly Dictionary<OutlineKey, WorldItemsOutlineRenderTarget> _cache = [];

    private int _drawsThisSecond;
    private int _secCounter;

    public override void PostUpdateEverything()
    {
        if (++_secCounter < 60)
            return;

        _secCounter = 0;
        //Log.Chat($"[Perf] WorldItemOutlines draws/s={_drawsThisSecond} keys={_cache.Count}");
        _drawsThisSecond = 0;
    }

    public bool TryGet(int type, int drawW, int drawH, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        target = null;
        origin = Vector2.Zero;

        int w = Math.Max(32, drawW + 32);
        int h = Math.Max(32, drawH + 32);

        OutlineKey key = new(type, border.PackedValue, w, h);

        if (!_cache.TryGetValue(key, out WorldItemsOutlineRenderTarget c))
        {
            c = new WorldItemsOutlineRenderTarget();
            _cache[key] = c;
            Main.ContentThatNeedsRenderTargets.Add(c);

            // Only request when created
            c.UseItem(type, w, h, border);
        }

        c.UseItem(type, w, h, border);

        target = c.GetOutlineTarget();
        if (target == null)
            return false;

        origin = new Vector2(target.Width * 0.5f, target.Height * 0.5f);

        _drawsThisSecond++; // perf counter
        return true;
    }

    public override void Unload()
    {
        if (!Main.dedServ)
        {
            foreach (var c in _cache.Values)
                Main.ContentThatNeedsRenderTargets.Remove(c);
        }

        _cache.Clear();
    }

    private readonly record struct OutlineKey(int Type, uint ColorPacked, int W, int H);
}


