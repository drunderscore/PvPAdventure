//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Terraria;
//using Terraria.Enums;
//using Terraria.GameContent;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines;

///// <summary>
///// Inspiration: https://github.com/GreatFriend129/VFXPlus/blob/4095bf8719021333825d8f71ff97e09590d06b1f/Content/Weapons/Magic/Hardmode/Staves/ShadowbeamStaff.cs#L22
///// </summary>
//internal class ProjectileShadowbeamOutlines : GlobalProjectile
//{
//    public override bool PreAI(Projectile projectile)
//    {
//        if (projectile.type != ProjectileID.ShadowBeamFriendly)
//            return true;

//        if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
//            return false;

//        Color color = Main.teamColor[(int)team];
//        color.A = 255;

//        projectile.localAI[0] += 1f;
//        projectile.alpha = 255;

//        if (projectile.localAI[0] > 9f)
//        {
//            for (int i = 0; i < 4; i++)
//            {
//                Vector2 position = projectile.position - projectile.velocity * (i * 0.25f);

//                Dust dust = Dust.NewDustPerfect(position, DustID.WhiteTorch, Vector2.Zero, 100, color, Main.rand.NextFloat(0.8f, 1.2f));
//                dust.noGravity = true;
//                dust.velocity = projectile.velocity * 0.2f;
//                dust.color = color;

//                //Vector2 vector33 = projectile.position;
//                //vector33 -= projectile.velocity * ((float)0 * 0.25f);
//                //int num378 = Dust.NewDust(vector33, 1, 1, DustID.Shadewood);
//                //Main.dust[num378].position = vector33;
//                //Main.dust[num378].scale = (float)Main.rand.Next(70, 110) * 0.013f;
//                //Dust dust94 = Main.dust[num378];
//                //Dust dust3 = dust94;
//                //dust3.velocity *= 0.2f;

//                //Dust dust = Dust.NewDustPerfect(position, 173, Vector2.Zero, 0, color, Main.rand.NextFloat(0.8f, 1.2f));
//                //dust.position = position;
//                //dust.color = color;
//                //dust.noLight = true;
//                //dust.noGravity = true;
//                //dust.velocity = projectile.velocity * 0.2f;
//            }

//            Lighting.AddLight(projectile.Center, color.ToVector3() * 0.5f);
//        }

//        return false;
//    }

//    public override bool PreDraw(Projectile projectile, ref Color lightColor)
//    {
//        if (projectile.type != ProjectileID.ShadowBeamFriendly)
//            return true;

//        if (!TeamProjectileOutlines.TryGetTeam(projectile, out Team team))
//            return false;

//        Color color = Main.teamColor[(int)team];
//        color.A = 0;

//        if (projectile.localAI[0] > 9f)
//        {
//            for (int i = 0; i < 4; i++)
//            {
//                Vector2 position = projectile.position - projectile.velocity * (i * 0.25f);
//                DrawShadowbeamParticle(position, color, Main.rand.NextFloat(0.8f, 1.2f));
//            }
//        }

//        Lighting.AddLight(projectile.Center, color.ToVector3() * 0.5f);
//        return false;
//    }

//    private static void DrawShadowbeamParticle(Vector2 position, Color color, float scale)
//    {
//        Texture2D texture = TextureAssets.MagicPixel.Value;

//        Main.EntitySpriteDraw(
//            texture,
//            position - Main.screenPosition,
//            null,
//            color * 0.9f,
//            Main.rand.NextFloat(MathHelper.TwoPi),
//            Vector2.One * 0.5f,
//            new Vector2(10f, 3f) * scale,
//            SpriteEffects.None);
//    }
//}
