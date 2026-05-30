using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines;

[Autoload(Side = ModSide.Client)]
internal sealed class TeamWhipOutlines : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (!IsWhip(projectile))
            return true;

        var outlines = ModContent.GetInstance<ClientConfig>().Outlines;
        if (!outlines.DrawOutlines || !outlines.ProjectileOutlines)
            return true;

        if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
            return true;

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        DrawWhipOutline(projectile, border);
        return true;
    }

    private static bool IsWhip(Projectile projectile)
    {
        if (projectile.type <= ProjectileID.None)
            return false;

        if (projectile.type < ProjectileID.Sets.IsAWhip.Length && ProjectileID.Sets.IsAWhip[projectile.type])
            return true;

        return projectile.aiStyle == ProjAIStyleID.Whip;
    }

    private static void DrawWhipOutline(Projectile projectile, Color border)
    {
        List<Vector2> points = [];
        Projectile.FillWhipControlPoints(projectile, points);

        if (points.Count < 2)
            return;

        Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
        int segmentCount = points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 position = points[i];
            Vector2 difference = points[i + 1] - position;

            if (difference == Vector2.Zero)
                continue;

            Rectangle frame = GetWhipFrame(texture, i, segmentCount);
            Vector2 origin = frame.Size() * 0.5f;
            float rotation = difference.ToRotation() - MathHelper.PiOver2;

            //DrawSegmentRing(texture, position, frame, origin, rotation, projectile.scale, 4, Color.Black);
            DrawSegmentRing(texture, position, frame, origin, rotation, projectile.scale, 2, border);
        }
    }

    private static Rectangle GetWhipFrame(Texture2D texture, int segmentIndex, int segmentCount)
    {
        const int whipFrames = 5;

        if (texture.Height % whipFrames != 0)
            return texture.Frame();

        int frameIndex = 1;

        if (segmentIndex == 0)
            frameIndex = 0;
        else if (segmentIndex >= segmentCount - 1)
            frameIndex = 4;
        else
        {
            float progress = segmentIndex / (float)Math.Max(1, segmentCount - 1);
            frameIndex = progress switch
            {
                > 0.66f => 3,
                > 0.33f => 2,
                _ => 1
            };
        }

        return texture.Frame(1, whipFrames, 0, frameIndex);
    }

    private static void DrawSegmentRing(Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, float rotation, float scale, int distance, Color color)
    {
        const int step = 2;

        for (int x = -distance; x <= distance; x += step)
        {
            for (int y = -distance; y <= distance; y += step)
            {
                if (Math.Abs(x) + Math.Abs(y) != distance)
                    continue;

                Main.EntitySpriteDraw(
                    texture,
                    position + new Vector2(x, y) - Main.screenPosition,
                    frame,
                    color,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None);
            }
        }
    }
}