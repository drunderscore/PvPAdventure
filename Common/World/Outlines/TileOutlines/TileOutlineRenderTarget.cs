using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.World.Outlines.TileOutlines;

// Create a render target for tiles.
internal sealed class TileOutlineRenderTarget : ARenderTargetContentByRequest
{
    private int _tileType;
    private int _tileW;
    private int _tileH;
    private int _coordinateWidth;
    private int _drawYOffset;
    private int _frameHash;
    private int[] _frameXs;
    private int[] _frameYs;
    private int[] _heights;
    private Color _borderColor;

    private RenderTarget2D _helperTarget;
    private EffectPass _colorOnlyPass;

    public void UseTile(int tileType, int tileW, int tileH, int coordinateWidth, int drawYOffset, int frameHash, int[] frameXs, int[] frameYs, int[] heights, Color borderColor)
    {
        if (_tileType == tileType && _tileW == tileW && _tileH == tileH && _coordinateWidth == coordinateWidth && _drawYOffset == drawYOffset && _frameHash == frameHash && _borderColor.PackedValue == borderColor.PackedValue)
        {
            if (!IsReady)
                Request();

            return;
        }

        _tileType = tileType;
        _tileW = tileW;
        _tileH = tileH;
        _coordinateWidth = coordinateWidth;
        _drawYOffset = drawYOffset;
        _frameHash = frameHash;
        _frameXs = frameXs;
        _frameYs = frameYs;
        _heights = heights;
        _borderColor = borderColor;

        Request();
    }

    public RenderTarget2D GetOutlineTarget() => _target;

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch sb)
    {
        Effect pixelShader = Main.pixelShader;
        _colorOnlyPass ??= pixelShader.CurrentTechnique.Passes["ColorOnly"];

        int contentW = _tileW * 16;
        int contentH = _tileH * 16;
        int w = Math.Max(32, contentW + 32 + Math.Max(0, _coordinateWidth - 16) * 2);
        int h = Math.Max(32, contentH + 32 + (Math.Abs(_drawYOffset) + Math.Max(0, MaxHeight() - 16)) * 2);

        PrepareARenderTarget_AndListenToEvents(ref _target, device, w, h, RenderTargetUsage.PreserveContents);
        PrepareARenderTarget_WithoutListeningToEvents(ref _helperTarget, device, w, h, RenderTargetUsage.DiscardContents);

        device.SetRenderTarget(_helperTarget);
        device.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        _colorOnlyPass.Apply();
        DrawTileMask(sb, w, h);
        pixelShader.CurrentTechnique.Passes[0].Apply();
        sb.End();

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        DrawOutline(sb);
        sb.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawTileMask(SpriteBatch sb, int w, int h)
    {
        Texture2D tex = TextureAssets.Tile[_tileType].Value;

        int contentW = _tileW * 16;
        int contentH = _tileH * 16;
        Vector2 topLeft = new Vector2((w - contentW) * 0.5f, (h - contentH) * 0.5f);

        for (int x = 0; x < _tileW; x++)
        {
            for (int y = 0; y < _tileH; y++)
            {
                int index = y * _tileW + x;
                Rectangle src = new(_frameXs[index], _frameYs[index], _coordinateWidth, Height(y));
                Vector2 dst = topLeft + new Vector2(x * 16, y * 16 + _drawYOffset);
                sb.Draw(tex, dst, src, Color.White);
            }
        }
    }

    private int Height(int y) => _heights != null && y < _heights.Length ? _heights[y] : 16;

    private int MaxHeight()
    {
        int max = 16;
        if (_heights != null)
            foreach (int height in _heights)
                max = Math.Max(max, height);

        return max;
    }

    private void DrawOutline(SpriteBatch sb)
    {
        if (_helperTarget == null)
            return;

        int step = 2;
        int dist = step * 2;

        for (int x = -dist; x <= dist; x += step)
        {
            for (int y = -dist; y <= dist; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) == dist)
                    sb.Draw(_helperTarget, new Vector2(x, y), Color.Black);
            }
        }

        dist = step;
        for (int x = -dist; x <= dist; x += step)
        {
            for (int y = -dist; y <= dist; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) == dist)
                    sb.Draw(_helperTarget, new Vector2(x, y), _borderColor);
            }
        }
    }
}
