using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Remove certain projectiles when their owner dies.
/// </summary>
public class DeadProjectileList : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.ClingerStaff ||
               entity.type == ProjectileID.SporeTrap ||
               entity.type == ProjectileID.SporeTrap2 ||
               entity.type == ProjectileID.SporeGas ||
               entity.type == ProjectileID.SporeGas2 ||
               entity.type == ProjectileID.RainCloudRaining ||
               entity.type == ProjectileID.BloodCloudRaining ||
               entity.type == ProjectileID.SporeGas3;
    }

    public override void PostAI(Projectile projectile)
    {
        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player owner = Main.player[projectile.owner];

        if (owner.dead || !owner.active)
        {
            projectile.Kill();
        }
    }
}
