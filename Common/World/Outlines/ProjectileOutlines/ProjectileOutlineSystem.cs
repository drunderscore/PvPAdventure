using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines;

[Autoload(Side = ModSide.Client)]
internal sealed class TeamProjectileOutlines : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        var outlines = ModContent.GetInstance<ClientConfig>().Outlines;
        if (projectile.ModProjectile is PortalCreationProjectile
            || !outlines.DrawOutlines
            || !outlines.ProjectileOutlines
            || ProjectileOutlineBanlist.IsBanned(projectile)   
            || !TryGetTeam(projectile, out Team team))
                    return true;

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        if (!ProjectileOutlineRenderTarget.TryGetFrame(projectile, out Rectangle frame))
            return true;

        Vector2 drawOrigin = GetProjectileDrawOrigin(projectile, frame);

        if (!ModContent.GetInstance<ProjectileOutlineSystem>().TryGet(projectile, frame, drawOrigin, border, out RenderTarget2D target, out Vector2 targetOrigin))
            return true;

        Color drawColor = lightColor * projectile.Opacity;
        drawColor.A = (byte)(255f * projectile.Opacity);

        SpriteEffects effects = projectile.spriteDirection == -1
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        float outlineRotation = projectile.rotation;

        // Custom rotation for shortswords
        if (projectile.aiStyle == ProjAIStyleID.ShortSword)
        {
            outlineRotation -= MathHelper.PiOver4;

            if (projectile.spriteDirection == -1)
            {
                outlineRotation += MathHelper.PiOver2;
            }
        }

        Main.spriteBatch.Draw(
            target,
            GetProjectileDrawPosition(projectile, outlineRotation),
            null,
            drawColor,
            outlineRotation,
            targetOrigin,
            projectile.scale,
            effects,
            0f);

        return true;
    }

    private static Vector2 GetProjectileDrawPosition(Projectile projectile, float rotation)
    {
        Vector2 position = projectile.Center - Main.screenPosition;
        position.Y += projectile.gfxOffY;

        // Jousting lance offset, right I think
        if (projectile.type is ProjectileID.JoustingLance or ProjectileID.ShadowJoustingLance or ProjectileID.HallowJoustingLance)
            position -= projectile.velocity.SafeNormalize(Vector2.Zero) * 60f * projectile.scale;

        // Spear offset, right!
        else if (projectile.aiStyle == ProjAIStyleID.Spear)
            position -= projectile.velocity.SafeNormalize(Vector2.Zero) * 40f * projectile.scale;

        // Offsets
        position += GetProjectileOutlineOffset(projectile).RotatedBy(rotation) * projectile.scale;

        // Pygmy, wrong
        //if (projectile.type == ProjectileID.Pygmy)
        //    position -= projectile.velocity.SafeNormalize(Vector2.Zero) * 40f * projectile.scale;

        //  Gungnir, wrong
        if (projectile.type == ProjectileID.Gungnir)
            position += (Main.GetPlayerArmPosition(projectile) - projectile.Center).SafeNormalize(Vector2.Zero) * 41f * projectile.scale;

        // Boomerang offset, wrong
        //if (projectile.aiStyle == ProjAIStyleID.Boomerang)
            //position += new Vector2(0f, 7f).RotatedBy(rotation) * projectile.scale;

        if (projectile.ModProjectile != null)
            position.X += projectile.ModProjectile.DrawOffsetX;

        return position;
    }

    private static Vector2 GetProjectileOutlineOffset(Projectile projectile)
    {
        return projectile.type switch
        {
            ProjectileID.ScarabBomb => new Vector2(0f, -9f),
            ProjectileID.Grenade => new Vector2(0f, 3f),
            ProjectileID.Bomb => new Vector2(0f, -4f),
            ProjectileID.FlaironBubble => new Vector2(2f, 3f),
            ProjectileID.Gungnir => new Vector2(-2f, -4f), // slightly wrong
            ProjectileID.Pygmy => new Vector2(projectile.spriteDirection * -6f, -9f), // slightly wrong
            ProjectileID.BabySlime => new Vector2(projectile.spriteDirection * 0f, -3f), // looks right, 99% sure
            ProjectileID.FrostHydra => new Vector2(projectile.spriteDirection * 6f, 3f),
            ProjectileID.Raven => new Vector2(projectile.spriteDirection * 1f, 1f),
            ProjectileID.FlyingImp => new Vector2(projectile.spriteDirection * 0f, -1f),
            ProjectileID.VenomSpider => new Vector2(projectile.spriteDirection * 1f, 2f),
            _ => Vector2.Zero
        };
    }

    private static Vector2 GetProjectileDrawOrigin(Projectile projectile, Rectangle frame)
    {
        Vector2 origin = frame.Size() * 0.5f;

        if (projectile.aiStyle == ProjAIStyleID.Boomerang)
            origin -= new Vector2(0f, 4f);

        if (projectile.ModProjectile != null)
        {
            origin.X += projectile.ModProjectile.DrawOriginOffsetX;
            origin.Y += projectile.ModProjectile.DrawOriginOffsetY;
        }

        return origin;
    }

    public static bool TryGetTeam(Projectile projectile, out Team team)
    {
        team = Team.None;
        if (!projectile.active || projectile.type <= ProjectileID.None || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return false;

        Player owner = Main.player[projectile.owner];
        team = owner?.active == true ? (Team)owner.team : Team.None;
        return team != Team.None;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ProjectileOutlineSystem : ModSystem
{
    private readonly Dictionary<Key, ProjectileOutlineRenderTarget> cache = [];

    public bool TryGet(Projectile projectile, Rectangle frame, Vector2 drawOrigin, Color border, out RenderTarget2D target, out Vector2 origin)
    {
        target = null;
        origin = Vector2.Zero;

        int w = Math.Max(32, frame.Width + ProjectileOutlineRenderTarget.Padding * 2);
        int h = Math.Max(32, frame.Height + ProjectileOutlineRenderTarget.Padding * 2);

        int originX2 = (int)MathF.Round(drawOrigin.X * 2f);
        int originY2 = (int)MathF.Round(drawOrigin.Y * 2f);

        Key key = new(projectile.type, frame.X, frame.Y, frame.Width, frame.Height, originX2, originY2, border.PackedValue);

        if (!cache.TryGetValue(key, out ProjectileOutlineRenderTarget rt))
        {
            rt = new ProjectileOutlineRenderTarget();
            cache[key] = rt;
            Main.ContentThatNeedsRenderTargets.Add(rt);
        }

        rt.UseProjectile(projectile.type, frame, drawOrigin, w, h, border);

        target = rt.GetOutlineTarget();

        if (target == null)
            return false;

        origin = rt.GetTargetOrigin();
        return true;
    }

    public override void Unload()
    {
        if (!Main.dedServ)
        {
            foreach (ProjectileOutlineRenderTarget rt in cache.Values)
                Main.ContentThatNeedsRenderTargets.Remove(rt);
        }

        cache.Clear();
    }

    private readonly record struct Key(int Type, int FrameX, int FrameY, int FrameW, int FrameH, int OriginX2, int OriginY2, uint Color);
}

internal sealed class ProjectileOutlineRenderTarget : ARenderTargetContentByRequest
{
    public const int Padding = 16;

    private int type;
    private int width;
    private int height;
    private Rectangle frame;
    private Vector2 drawOrigin;
    private Color border;
    private RenderTarget2D helperTarget;
    private EffectPass colorOnlyPass;

    public void UseProjectile(int type, Rectangle frame, Vector2 drawOrigin, int width, int height, Color border)
    {
        if (this.type == type && this.frame == frame && this.drawOrigin == drawOrigin && this.width == width && this.height == height && this.border.PackedValue == border.PackedValue)
        {
            if (!IsReady)
                Request();

            return;
        }

        this.type = type;
        this.frame = frame;
        this.drawOrigin = drawOrigin;
        this.width = width;
        this.height = height;
        this.border = border;

        Request();
    }

    public RenderTarget2D GetOutlineTarget() => _target;

    public Vector2 GetTargetOrigin() => new(Padding + drawOrigin.X, Padding + drawOrigin.Y);

    public static bool TryGetFrame(Projectile projectile, out Rectangle frame)
    {
        frame = default;

        if (projectile.type <= ProjectileID.None || projectile.type >= TextureAssets.Projectile.Length)
            return false;

        Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
        int frames = Math.Max(1, Main.projFrames[projectile.type]);
        int frameIndex = Math.Clamp(projectile.frame, 0, frames - 1);

        frame = texture.Frame(1, frames, 0, frameIndex);

        return frame.Width > 0 && frame.Height > 0;
    }

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        Effect shader = Main.pixelShader;
        colorOnlyPass ??= shader.CurrentTechnique.Passes["ColorOnly"];

        PrepareARenderTarget_AndListenToEvents(ref _target, device, width, height, RenderTargetUsage.PreserveContents);
        PrepareARenderTarget_WithoutListeningToEvents(ref helperTarget, device, width, height, RenderTargetUsage.DiscardContents);

        device.SetRenderTarget(helperTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        colorOnlyPass.Apply();
        DrawMask(spriteBatch);
        shader.CurrentTechnique.Passes[0].Apply();
        spriteBatch.End();

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

        // --- Actual draw call ---
        DrawOutline(spriteBatch);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawMask(SpriteBatch spriteBatch)
    {
        Texture2D texture = TextureAssets.Projectile[type].Value;

        spriteBatch.Draw(
            texture,
            GetTargetOrigin(),
            frame,
            Color.White,
            0f,
            drawOrigin,
            1f,
            SpriteEffects.None,
            0f);
    }

    private void DrawOutline(SpriteBatch spriteBatch)
    {
        if (helperTarget == null)
            return;

        DrawRing(spriteBatch, 4, Color.Black);
        DrawRing(spriteBatch, 2, border);
    }

    private void DrawRing(SpriteBatch spriteBatch, int distance, Color color)
    {
        const int step = 2;

        for (int x = -distance; x <= distance; x += step)
        {
            for (int y = -distance; y <= distance; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) == distance)
                    spriteBatch.Draw(helperTarget, new Vector2(x, y), color);
            }
        }
    }
}
