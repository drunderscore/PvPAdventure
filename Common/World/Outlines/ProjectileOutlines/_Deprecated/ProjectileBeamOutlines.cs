//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using PvPAdventure.Content.Portals;
//using PvPAdventure.Core.Config;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;
//using Team = Terraria.Enums.Team;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines;

//[Autoload(Side = ModSide.Client)]
//internal sealed class ProjectileBeamOutlines : ModSystem
//{
//    public override void Load()
//    {
//        On_Main.DrawCachedProjs += OnDrawCachedProjs;
//    }

//    public override void Unload()
//    {
//        On_Main.DrawCachedProjs -= OnDrawCachedProjs;
//    }

//    private static void OnDrawCachedProjs(
//        On_Main.orig_DrawCachedProjs orig,
//        Main self,
//        List<int> projCache,
//        bool startSpriteBatch)
//    {
//        orig(self, projCache, startSpriteBatch);

//        var outlines = ModContent.GetInstance<ClientConfig>().Outlines;
//        if (!outlines.DrawOutlines || !outlines.ProjectileOutlines)
//            return;

//        var system = ModContent.GetInstance<ProjectileOutlineSystem>();

//        foreach (int i in projCache)
//        {
//            //Log.Chat(i);

//            Projectile projectile = Main.projectile[i];

//            if (projectile.ModProjectile is PortalCreationProjectile)
//                continue;

//            if (ProjectileOutlineBanlist.IsBanned(projectile))
//                continue;

//            // Skip actual beam-style projectiles — they are drawn as tiled
//            // segments along a path and have no single sprite at Center to outline.
//            if (projectile.aiStyle == ProjAIStyleID.Beam)
//                continue;

//            if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
//                continue;

//            Color border = Main.teamColor[(int)team];
//            border.A = 255;

//            if (!ProjectileOutlineRenderTarget.TryGetFrame(projectile, out Rectangle frame))
//                continue;

//            Vector2 drawOrigin = GetProjectileDrawOrigin(projectile, frame);

//            if (!system.TryGet(projectile, frame, drawOrigin, border, out RenderTarget2D target, out Vector2 targetOrigin))
//                continue;

//            Color lightColor = Lighting.GetColor(projectile.Center.ToTileCoordinates());
//            Color drawColor = lightColor * projectile.Opacity;
//            drawColor.A = (byte)(255f * projectile.Opacity);

//            SpriteEffects effects = projectile.spriteDirection == -1
//                ? SpriteEffects.FlipHorizontally
//                : SpriteEffects.None;

//            Main.spriteBatch.Draw(
//                target,
//                GetProjectileDrawPosition(projectile),
//                null,
//                drawColor,
//                projectile.rotation,
//                targetOrigin,
//                projectile.scale,
//                effects,
//                0f);
//        }
//    }

//    private static Vector2 GetProjectileDrawPosition(Projectile projectile)
//    {
//        Vector2 position = projectile.Center - Main.screenPosition;
//        position.Y += projectile.gfxOffY;

//        if (projectile.ModProjectile != null)
//            position.X += projectile.ModProjectile.DrawOffsetX;

//        return position;
//    }

//    private static Vector2 GetProjectileDrawOrigin(Projectile projectile, Rectangle frame)
//    {
//        Vector2 origin = frame.Size() * 0.5f;

//        if (projectile.ModProjectile != null)
//        {
//            origin.X += projectile.ModProjectile.DrawOriginOffsetX;
//            origin.Y += projectile.ModProjectile.DrawOriginOffsetY;
//        }

//        return origin;
//    }
//}