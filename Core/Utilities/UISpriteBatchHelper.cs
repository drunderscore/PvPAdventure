using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace PvPAdventure.Core.Utilities;

public static class UISpriteBatchHelper
{
    private static RasterizerState clippedRasterizer;

    private static RasterizerState ClippedRasterizer => clippedRasterizer ??= new RasterizerState
    {
        CullMode = CullMode.CullCounterClockwiseFace,
        FillMode = FillMode.Solid,
        ScissorTestEnable = true
    };

    public static void Restart(
        SpriteBatch spriteBatch,
        BlendState blendState,
        SamplerState samplerState = null,
        Effect effect = null,
        SpriteSortMode sortMode = SpriteSortMode.Deferred)
    {
        GraphicsDevice device = spriteBatch.GraphicsDevice;
        Rectangle scissor = device.ScissorRectangle;

        if (scissor.Width <= 0 || scissor.Height <= 0)
            scissor = device.Viewport.Bounds;

        spriteBatch.End();
        spriteBatch.Begin(
            sortMode,
            blendState,
            samplerState ?? SamplerState.LinearClamp,
            DepthStencilState.None,
            ClippedRasterizer,
            effect,
            Main.UIScaleMatrix);

        device.ScissorRectangle = scissor;
    }
}
