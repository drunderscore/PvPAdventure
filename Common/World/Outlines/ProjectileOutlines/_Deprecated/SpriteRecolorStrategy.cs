//using Microsoft.Xna.Framework;
//using Terraria;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines.Strategies;

//internal sealed class SpriteRecolorStrategy : IProjectileRecolorStrategy
//{
//    public Color? GetAlpha(Projectile projectile, Color lightColor, Color teamColor)
//    {
//        Color c = teamColor;
//        c.A = (byte)(lightColor.A * projectile.Opacity);
//        return c * projectile.Opacity;
//    }
//}