using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Removes knockback from specific projectiles.
/// </summary>
public class NoKnockbackProjectiles : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        int projType = entity.type;
        return projType == ProjectileID.Meteor1 ||
               projType == ProjectileID.Meteor2 ||
               projType == ProjectileID.Meteor3 ||
               projType == ProjectileID.ToxicCloud ||
               projType == ProjectileID.ToxicCloud2 ||
               projType == ProjectileID.ToxicCloud3 ||
               projType == ProjectileID.HeatRay ||
               projType == ProjectileID.RainbowBack ||
               projType == ProjectileID.RainbowFront ||
               projType == ProjectileID.TinyEater ||
               projType == ProjectileID.FlyingKnife ||
               projType == ProjectileID.LaserMachinegunLaser ||
               projType == ProjectileID.ClingerStaff ||
               projType == ProjectileID.SporeTrap ||
               projType == ProjectileID.SporeTrap2 ||
               projType == ProjectileID.SporeGas ||
               projType == ProjectileID.SporeGas2 ||
               projType == ProjectileID.RainCloudRaining ||
               projType == ProjectileID.Electrosphere ||
               projType == ProjectileID.InfernoFriendlyBlast ||
               projType == ProjectileID.FlaironBubble ||
               projType == ProjectileID.Volcano ||
               projType == ProjectileID.Muramasa ||
               projType == ProjectileID.BloodCloudRaining ||
               projType == ProjectileID.Volcano ||
               projType == ProjectileID.CrystalLeafShot ||
               projType == ProjectileID.SporeGas3;
    }
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.Knockback *= 0f;
    }
}
