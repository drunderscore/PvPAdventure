using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Projectiles;

/// <summary>
/// Remove certain projectiles when their owner dies.
/// </summary>
public sealed class DeadProjectileList : GlobalProjectile
{
    private static readonly HashSet<int> KillOnOwnerDeath = new()
    {
        ProjectileID.ClingerStaff,
        ProjectileID.SporeTrap,
        ProjectileID.SporeTrap2,
        ProjectileID.SporeGas,
        ProjectileID.SporeGas2,
        ProjectileID.SporeGas3,
        ProjectileID.RainCloudRaining,
        ProjectileID.BloodCloudRaining
    };

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return KillOnOwnerDeath.Contains(entity.type);
    }

    public override void PostAI(Projectile projectile)
    {
        // Server authority only
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player owner = Main.player[projectile.owner];

        if (!owner.active || owner.dead)
        {
            projectile.Kill();
        }
    }
}
