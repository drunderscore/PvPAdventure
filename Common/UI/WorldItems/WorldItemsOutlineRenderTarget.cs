using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.UI.WorldItems;

internal sealed class WorldItemsOutlineRenderTarget : ARenderTargetContentByRequest
{
    private int _itemType;
    private int _width;
    private int _height;
    private Color _borderColor;
    private RenderTarget2D _helperTarget;
    private EffectPass _colorOnlyPass;

    public void UseItem(int itemType, int width, int height, Color borderColor)
    {
        if (_itemType == itemType && _width == width && _height == height && _borderColor.PackedValue == borderColor.PackedValue)
        {
            if (!IsReady)
                Request();

            return;
        }

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
        //PrepareARenderTarget_AndListenToEvents(ref _target, device, _width, _height, RenderTargetUsage.PreserveContents);
        PrepareARenderTarget_AndListenToEvents(ref _target, device, _width, _height, RenderTargetUsage.DiscardContents);
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




