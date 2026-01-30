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
    private int _frameX;
    private int _frameY;
    private int _tileW;
    private int _tileH;
    private Color _borderColor;

    private RenderTarget2D _helperTarget;
    private EffectPass _colorOnlyPass;

    public void UseTile(int tileType, int frameX, int frameY, int tileW, int tileH, Color borderColor)
    {
        if (_tileType == tileType && _frameX == frameX && _frameY == frameY && _tileW == tileW && _tileH == tileH && _borderColor.PackedValue == borderColor.PackedValue)
        {
            if (!IsReady)
                Request();

            return;
        }

        _tileType = tileType;
        _frameX = frameX;
        _frameY = frameY;
        _tileW = tileW;
        _tileH = tileH;
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
        int w = Math.Max(32, contentW + 32);
        int h = Math.Max(32, contentH + 32);

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

        const int stride = 18; // tilesheet cell stride

        for (int x = 0; x < _tileW; x++)
        {
            for (int y = 0; y < _tileH; y++)
            {
                Rectangle src = new(_frameX + x * stride, _frameY + y * stride, 16, 16);
                Vector2 dst = topLeft + new Vector2(x * 16, y * 16);
                sb.Draw(tex, dst, src, Color.White);
            }
        }
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
