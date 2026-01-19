using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Projectiles;

/// <summary>
/// Grants buffs to whip users in PvP when they hit a player.
/// </summary>
public class WhipBuffs : GlobalProjectile
{
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        if (projectile.type == ProjectileID.SwordWhip && target.hostile)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player attacker = Main.player[projectile.owner];
                if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                {
                    int buffDuration = 420;
                    attacker.AddBuff(BuffID.SwordWhipPlayerBuff, buffDuration);
                }
            }
        }
        if (projectile.type == ProjectileID.ThornWhip && target.hostile)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player attacker = Main.player[projectile.owner];

                if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                {
                    int buffDuration = 420; // 7 seconds
                    attacker.AddBuff(BuffID.ThornWhipPlayerBuff, buffDuration);
                }
            }
        }
        if (projectile.type == ProjectileID.ScytheWhip && target.hostile)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player attacker = Main.player[projectile.owner];

                if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                {
                    int buffDuration = 420;
                    attacker.AddBuff(BuffID.ScytheWhipPlayerBuff, buffDuration);
                }
            }
        }
        if (projectile.type == ProjectileID.CoolWhip && target.hostile)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player attacker = Main.player[projectile.owner];

                if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                {
                    int buffDuration = 420;
                    attacker.AddBuff(BuffID.CoolWhipPlayerBuff, buffDuration);
                }
            }
        }
    }
}
