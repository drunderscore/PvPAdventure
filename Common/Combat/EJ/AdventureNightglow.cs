using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Makes the Fairy Queen's magic shots home in on the cursor after a short delay.
/// </summary>
public class AdventureNightglow : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.FairyQueenMagicItemShot;

    public override void SetDefaults(Projectile entity)
    {
        entity.localAI[0] = 0;
    }

    public override void AI(Projectile projectile)
    {
        if (projectile.localAI[0] <= 60)
        {
            projectile.localAI[0]++;
            return;
        }

        if (!projectile.TryGetOwner(out var owner))
            return;

        if (owner.whoAmI != Main.myPlayer)
            return;

        if (owner.itemAnimation > 0 && owner.HeldItem.type == ItemID.FairyQueenMagicItem)
        {
            var cursorPosition = Main.MouseWorld;
            var toCursor = cursorPosition - projectile.Center;

            var baseSpeed = 20.0f;
            var accelerationFactor = 5.5f;
            var turnStrength = 0.35f;

            var direction = toCursor.SafeNormalize(Vector2.Zero);
            var targetVelocity = direction * baseSpeed * accelerationFactor;

            projectile.velocity = Vector2.Lerp(projectile.velocity, targetVelocity, turnStrength);
            projectile.rotation = projectile.velocity.ToRotation() * MathHelper.PiOver2;
            projectile.netUpdate = true;
        }
    }
}
