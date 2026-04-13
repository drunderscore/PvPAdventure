using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Projectiles;

/// <summary>
/// The hitbox of the PowerBombs
/// </summary>
internal class PowerBombExplosion : ModProjectile
{
    public override string Texture => "PvPAdventure/Assets/Projectiles/PowerBombProjectile";

    private const int HitboxLifetime = 3;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 1;
    }

    public override void SetDefaults()
    {
        Projectile.width = 9 * 16 * 2;
        Projectile.height = 9 * 16 * 2;

        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.timeLeft = HitboxLifetime;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = 0;

        // invis projectile
        Projectile.alpha = 255;
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
    }

    public override bool CanHitPlayer(Player target) => false;

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Vector2 direction = target.Center - Projectile.Center;
        if (direction == Vector2.Zero)
            direction = Vector2.UnitY;

        modifiers.HitDirectionOverride = direction.X > 0 ? 1 : -1;
    }
}