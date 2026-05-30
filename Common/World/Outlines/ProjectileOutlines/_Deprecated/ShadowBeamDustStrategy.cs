//using Microsoft.Xna.Framework;
//using Terraria;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines.Strategies;

//internal sealed class ShadowBeamDustStrategy : IProjectileRecolorStrategy
//{
//    public bool PreAI(Projectile projectile, Color teamColor)
//    {
//        projectile.localAI[0] += 1f;
//        projectile.alpha = 255;

//        if (projectile.localAI[0] <= 9f)
//            return false;

//        for (int i = 0; i < 4; i++)
//        {
//            Vector2 position = projectile.position;
//            position -= projectile.velocity * (i * 0.25f);

//            int dustIndex = Dust.NewDust(position, 1, 1, 173);
//            Dust dust = Main.dust[dustIndex];

//            dust.position = position;
//            dust.color = teamColor;
//            dust.scale = Main.rand.Next(70, 110) * 0.013f;
//            dust.velocity *= 0.2f;
//        }

//        Lighting.AddLight(projectile.Center, teamColor.ToVector3() * 0.5f);
//        return false;
//    }
//}