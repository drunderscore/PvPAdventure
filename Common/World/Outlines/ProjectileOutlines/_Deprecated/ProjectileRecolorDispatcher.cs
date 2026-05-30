//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using PvPAdventure.Common.World.Outlines.ProjectileOutlines;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;
//using Team = Terraria.Enums.Team;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines.Strategies;

//[Autoload(Side = ModSide.Client)]
//internal sealed class ProjectileRecolorDispatcher : GlobalProjectile
//{
//    public override bool PreAI(Projectile projectile)
//    {
//        if (projectile.type == ProjectileID.ShadowBeamFriendly)
//        {
//            if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
//                return true;

//            Color color = Main.teamColor[(int)team];
//            color.A = 255;

//            projectile.localAI[0] += 1f;
//            projectile.alpha = 255;

//            if (projectile.localAI[0] > 9f)
//            {
//                for (int i = 0; i < 4; i++)
//                {
//                    Vector2 position = projectile.position;
//                    position -= projectile.velocity * (i * 0.25f);

//                    int dustIndex = Dust.NewDust(position, 1, 1, 173);
//                    Dust dust = Main.dust[dustIndex];

//                    dust.position = position;
//                    dust.color = color;
//                    dust.scale = Main.rand.Next(70, 110) * 0.013f;
//                    dust.velocity *= 0.2f;
//                }

//                Lighting.AddLight(projectile.Center, color.ToVector3() * 0.5f);
//            }

//            return false;
//        }

//        if (!TryGetStrategyAndColor(projectile, out var strategy, out Color teamColor))
//            return true;

//        return strategy.PreAI(projectile, teamColor);
//    }

//    public override void PostAI(Projectile projectile)
//    {
//        if (!TryGetStrategyAndColor(projectile, out var strategy, out Color teamColor))
//            return;

//        strategy.PostAI(projectile, teamColor);
//    }

//    public override Color? GetAlpha(Projectile projectile, Color lightColor)
//    {
//        if (!TryGetStrategyAndColor(projectile, out var strategy, out Color teamColor))
//            return null;

//        return strategy.GetAlpha(projectile, lightColor, teamColor);
//    }

//    public override bool PreDraw(Projectile projectile, ref Color lightColor)
//    {
//        if (!TryGetStrategyAndColor(projectile, out var strategy, out Color teamColor))
//            return true;

//        return strategy.PreDraw(projectile, ref lightColor, teamColor);
//    }

//    private static bool TryGetStrategyAndColor(
//        Projectile projectile,
//        out IProjectileRecolorStrategy strategy,
//        out Color teamColor)
//    {
//        strategy = null;
//        teamColor = default;

//        if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
//            return false;

//        if (!ProjectileRecolorRegistry.Instance.TryGet(projectile, out strategy))
//            return false;

//        teamColor = Main.teamColor[(int)team];
//        teamColor.A = 255;
//        return true;
//    }
//}