using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    public override bool? CanCutTiles(Projectile projectile)
    {
        if (projectile.owner == Main.myPlayer && Player.tileTargetX >= 3200)
            return false;

        return null;
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