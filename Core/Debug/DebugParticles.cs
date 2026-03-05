using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal sealed class DebugTrueExcaliburProjIds : GlobalProjectile
{
    public override void AI(Projectile projectile)
    {
        if (projectile.owner != Main.myPlayer)
            return;

        // Temporarily log melee-ish projectiles when you swing/hit.
        // Replace with your own filtering if needed.
        if (projectile.timeLeft == 1)
            return;

        if (projectile.DamageType != null && projectile.DamageType.CountsAsClass(Terraria.ModLoader.DamageClass.Melee))
        {
            // Uncomment to see which projectile types are involved (spammy).
             //Log.Chat($"Proj {projectile.type} aiStyle={projectile.aiStyle}");
        }
        //Log.Debug(projectile.DamageType);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        //return false;
        return base.PreDraw(projectile, ref lightColor);
    }
    public override bool PreDrawExtras(Projectile projectile)
    {
        //return false;
        return base.PreDrawExtras(projectile);
    }
}
#endif