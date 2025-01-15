using Microsoft.Xna.Framework;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    public override bool? CanCutTiles(Projectile projectile)
    {
        if (projectile.owner == Main.myPlayer)
        {
            var region = ModContent.GetInstance<RegionManager>()
                .GetRegionIntersecting(projectile.Hitbox.ToTileRectangle());

            if (region != null && !region.CanModifyTiles)
                return false;
        }

        return null;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        if (projectile.type == ProjectileID.RainbowRodBullet)
            projectile.Kill();
        return true;
    }

    public override void SetDefaults(Projectile entity)
    {
        // All projectiles are important.
        entity.netImportant = true;
    }

    public override void PostAI(Projectile projectile)
    {
        // Ignore net spam restraints.
        projectile.netSpam = 0;
    }
}