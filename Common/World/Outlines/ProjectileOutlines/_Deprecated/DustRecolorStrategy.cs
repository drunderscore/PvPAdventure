//using Microsoft.Xna.Framework;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.ID;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines.Strategies;

//internal sealed class DustRecolorStrategy(int[] dustTypes, float proximityRadius)
//    : IProjectileRecolorStrategy
//{
//    private readonly HashSet<int> dustTypeSet = [.. dustTypes];
//    private readonly float radiusSq = proximityRadius * proximityRadius;

//    public void PostAI(Projectile projectile, Color teamColor)
//    {
//        for (int i = 0; i < Main.maxDust; i++)
//        {
//            Dust dust = Main.dust[i];
//            if (!dust.active || !dustTypeSet.Contains(dust.type))
//                continue;

//            //if (projectile.type == ProjectileID.ShadowBeamFriendly && dust.active && Vector2.DistanceSquared(dust.position, projectile.Center) <= radiusSq)
//                //Log.Chat($"Shadowbeam nearby dust: type={dust.type}");

//            if (Vector2.DistanceSquared(dust.position, projectile.Center) > radiusSq)
//                continue;

//            dust.color = teamColor;
//        }

//        Lighting.AddLight(projectile.Center, teamColor.ToVector3() * 0.5f);
//    }
//}