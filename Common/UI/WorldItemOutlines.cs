using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI;

internal sealed class WorldItemOutlines : ARenderTargetContentByRequest
{
    private int _itemType;
    private int _width;
    private int _height;
    private Color _borderColor;
    private RenderTarget2D _helperTarget;
    private EffectPass _colorOnlyPass;

    public void UseItem(int itemType, int width, int height, Color borderColor)
    {
        _itemType = itemType;
        _width = width;
        _height = height;
        _borderColor = borderColor;
        Request();
    }

    public RenderTarget2D GetOutlineTarget() => _target;

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        // Create a pixel shader
        Effect pixelShader = Main.pixelShader;
        _colorOnlyPass ??= pixelShader.CurrentTechnique.Passes["ColorOnly"];

        // Prepare render target
        PrepareARenderTarget_AndListenToEvents(ref _target, device, _width, _height, RenderTargetUsage.PreserveContents);
        PrepareARenderTarget_WithoutListeningToEvents(ref _helperTarget, device, _width, _height, RenderTargetUsage.DiscardContents);

        // Draw item mask
        device.SetRenderTarget(_helperTarget);
        device.Clear(Color.Transparent);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null);
        _colorOnlyPass.Apply();
        DrawItemMask(spriteBatch);
        pixelShader.CurrentTechnique.Passes[0].Apply();
        spriteBatch.End();

        // Draw outline
        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null);
        DrawOutline(spriteBatch);
        spriteBatch.End();

        // Reset
        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawItemMask(SpriteBatch spriteBatch)
    {
        if (_itemType <= 0)
            return;

        Texture2D tex = TextureAssets.Item[_itemType].Value;
        Rectangle frame = Main.itemAnimations[_itemType] != null ? Main.itemAnimations[_itemType].GetFrame(tex) : new Rectangle(0, 0, tex.Width, tex.Height);

        Vector2 pos = new(_width * 0.5f, _height * 0.5f);
        Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);

        spriteBatch.Draw(tex, pos, frame, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
    }

    private void DrawOutline(SpriteBatch sb)
    {
        if (_helperTarget == null)
            return;

        // Adjust thickness of outline
        int step = 2; 
        int dist = step * 2;

        // Draw black
        Color black = Color.Black;
        for (int x = -dist; x <= dist; x += step)
        {
            for (int y = -dist; y <= dist; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) == dist)
                    sb.Draw(_helperTarget, new Vector2(x, y), black);
            }
        }

        // Draw border
        Color border = _borderColor;
        dist = step;
        for (int x = -dist; x <= dist; x += step)
        {
            for (int y = -dist; y <= dist; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) == dist)
                    sb.Draw(_helperTarget, new Vector2(x, y), border);
            }
        }
    }
}


[Autoload(Side = ModSide.Client)]
internal sealed class WorldItemOutlineRenderTargetSystem : ModSystem
{
    // Store item outlines by key
    private readonly Dictionary<OutlineKey, WorldItemOutlines> _cache = [];

    public bool TryGet(int type, int drawW, int drawH, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        target = null;
        origin = Vector2.Zero;

        int w = Math.Max(32, drawW + 32);
        int h = Math.Max(32, drawH + 32);

        OutlineKey key = new(type, border.PackedValue, w, h);

        if (!_cache.TryGetValue(key, out WorldItemOutlines c))
        {
            c = new WorldItemOutlines();
            _cache[key] = c;
            Main.ContentThatNeedsRenderTargets.Add(c);
        }

        c.UseItem(type, w, h, border);

        if (!c.IsReady || c._target == null)
            return false;

        target = c.GetOutlineTarget();
        origin = new Vector2(target.Width * 0.5f, target.Height * 0.5f);
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


