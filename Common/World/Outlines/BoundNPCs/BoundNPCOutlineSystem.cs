
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPFramework.Common.World.Outlines.BoundNPCs;

[Autoload(Side=ModSide.Client)]
internal sealed class BoundNpcOutlineSystem : ModSystem
{
    private readonly Dictionary<Key, BoundNpcOutlineRenderTarget> cache = [];

    public bool TryGet(Terraria.NPC npc, Color borderColor, out Microsoft.Xna.Framework.Graphics.RenderTarget2D target, out Microsoft.Xna.Framework.Vector2 origin)
    {
        target = null;
        origin = Microsoft.Xna.Framework.Vector2.Zero;

        Microsoft.Xna.Framework.Rectangle frame = npc.frame;

        if (frame.Width <= 0 || frame.Height <= 0)
        {
            Microsoft.Xna.Framework.Graphics.Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
            int frameCount = Terraria.Main.npcFrameCount[npc.type];
            frame = texture.Frame(1, frameCount > 0 ? frameCount : 1);
        }

        int width = System.Math.Max(32, frame.Width + 32);
        int height = System.Math.Max(32, frame.Height + 32);

        Key key = new(npc.type, frame.X, frame.Y, frame.Width, frame.Height, borderColor.PackedValue);

        if (!cache.TryGetValue(key, out BoundNpcOutlineRenderTarget renderTarget))
        {
            renderTarget = new BoundNpcOutlineRenderTarget();
            cache[key] = renderTarget;
            Terraria.Main.ContentThatNeedsRenderTargets.Add(renderTarget);
        }

        renderTarget.UseNpc(npc.type, frame, width, height, borderColor);

        target = renderTarget.GetOutlineTarget();

        if (target == null)
            return false;

        origin = new Microsoft.Xna.Framework.Vector2(target.Width * 0.5f, target.Height * 0.5f);

        return true;
    }

    public override void Unload()
    {
        if (!Terraria.Main.dedServ)
        {
            foreach (BoundNpcOutlineRenderTarget renderTarget in cache.Values)
                Terraria.Main.ContentThatNeedsRenderTargets.Remove(renderTarget);
        }

        cache.Clear();
    }

    private readonly record struct Key(int NpcType, int FrameX, int FrameY, int FrameWidth, int FrameHeight, uint ColorPacked);
}

internal sealed class BoundNpcOutlineRenderTarget : ARenderTargetContentByRequest
{
    private int npcType;
    private int width;
    private int height;
    private Microsoft.Xna.Framework.Rectangle frame;
    private Microsoft.Xna.Framework.Color borderColor;

    private Microsoft.Xna.Framework.Graphics.RenderTarget2D helperTarget;
    private Microsoft.Xna.Framework.Graphics.EffectPass colorOnlyPass;

    public void UseNpc(int npcType, Microsoft.Xna.Framework.Rectangle frame, int width, int height, Microsoft.Xna.Framework.Color borderColor)
    {
        if (this.npcType == npcType && this.frame == frame && this.width == width && this.height == height && this.borderColor.PackedValue == borderColor.PackedValue)
        {
            if (!IsReady)
                Request();

            return;
        }

        this.npcType = npcType;
        this.frame = frame;
        this.width = width;
        this.height = height;
        this.borderColor = borderColor;

        Request();
    }

    public Microsoft.Xna.Framework.Graphics.RenderTarget2D GetOutlineTarget() => _target;

    protected override void HandleUseReqest(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        Microsoft.Xna.Framework.Graphics.Effect pixelShader = Terraria.Main.pixelShader;
        colorOnlyPass ??= pixelShader.CurrentTechnique.Passes["ColorOnly"];

        PrepareARenderTarget_AndListenToEvents(ref _target, device, width, height, Microsoft.Xna.Framework.Graphics.RenderTargetUsage.PreserveContents);
        PrepareARenderTarget_WithoutListeningToEvents(ref helperTarget, device, width, height, Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);

        device.SetRenderTarget(helperTarget);
        device.Clear(Microsoft.Xna.Framework.Color.Transparent);

        spriteBatch.Begin(
            Microsoft.Xna.Framework.Graphics.SpriteSortMode.Immediate,
            Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
            Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp,
            Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
            Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise,
            null);

        colorOnlyPass.Apply();
        DrawNpcMask(spriteBatch);
        pixelShader.CurrentTechnique.Passes[0].Apply();

        spriteBatch.End();

        device.SetRenderTarget(_target);
        device.Clear(Microsoft.Xna.Framework.Color.Transparent);

        spriteBatch.Begin(
            Microsoft.Xna.Framework.Graphics.SpriteSortMode.Immediate,
            Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
            Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp,
            Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
            Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise,
            null);

        DrawOutline(spriteBatch);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawNpcMask(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (npcType <= 0)
            return;

        Microsoft.Xna.Framework.Graphics.Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npcType].Value;
        Microsoft.Xna.Framework.Vector2 position = new(width * 0.5f, height * 0.5f);
        Microsoft.Xna.Framework.Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);

        spriteBatch.Draw(texture, position, frame, Microsoft.Xna.Framework.Color.White, 0f, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
    }

    private void DrawOutline(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (helperTarget == null)
            return;

        const int step = 2;

        int distance = step * 2;

        for (int x = -distance; x <= distance; x += step)
        {
            for (int y = -distance; y <= distance; y += step)
            {
                if (System.Math.Abs(x) + System.Math.Abs(y) == distance)
                    spriteBatch.Draw(helperTarget, new Microsoft.Xna.Framework.Vector2(x, y), Microsoft.Xna.Framework.Color.Black);
            }
        }

        distance = step;

        for (int x = -distance; x <= distance; x += step)
        {
            for (int y = -distance; y <= distance; y += step)
            {
                if (System.Math.Abs(x) + System.Math.Abs(y) == distance)
                    spriteBatch.Draw(helperTarget, new Microsoft.Xna.Framework.Vector2(x, y), borderColor);
            }
        }
    }
}
