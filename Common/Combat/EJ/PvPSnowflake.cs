using System;
using Microsoft.Xna.Framework;
using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace PvPAdventure.Common.Combat.EJ;
#nullable enable
/// <summary>
/// projectile AI changes for the coolwhip snowflake that allow it to target players in PvP, as well as letting it spawn in PvP like how it it spawns in PvE, with logic taken directly from vanilla code
/// </summary>
public class PvPSnowflake : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.CoolWhipProj;

    public override bool InstancePerEntity => true;

    private const float PvPTargetRangeTiles = 100f;
    private const float PvPTargetRangeSq = (PvPTargetRangeTiles * 16f) * (PvPTargetRangeTiles * 16f);
    private const int HitCooldownFrames = 30;
    private readonly int[] _playerHitCooldown = new int[Main.maxPlayers];

    public override bool PreAI(Projectile projectile)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (_playerHitCooldown[i] > 0)
                _playerHitCooldown[i]--;
        }

        if (!projectile.TryGetOwner(out Player? owner) || owner is null || !owner.active || owner.dead)
            return true;

        if (owner.whoAmI != Main.myPlayer)
            return true;

        Player? pvpTarget = FindPvPTarget(projectile, owner);
        if (pvpTarget == null)
            return true;

        if (!owner.HasBuff(BuffID.CoolWhipPlayerBuff))
        {
            projectile.Kill();
            return false;
        }

        if (projectile.Hitbox.Intersects(pvpTarget.Hitbox))
            _playerHitCooldown[pvpTarget.whoAmI] = HitCooldownFrames;

        RunPvPHomingAI(projectile, pvpTarget);
        return false;
    }

    private Player? FindPvPTarget(Projectile projectile, Player owner)
    {
        Player? best = null;
        float bestDistSq = PvPTargetRangeSq;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (_playerHitCooldown[i] > 0)
                continue;

            Player candidate = Main.player[i];
            if (!IsValidTarget(candidate, owner))
                continue;

            float distSq = Vector2.DistanceSquared(projectile.Center, candidate.Center);
            if (distSq <= bestDistSq)
            {
                bestDistSq = distSq;
                best = candidate;
            }
        }

        return best;
    }

    private static void RunPvPHomingAI(Projectile projectile, Player target)
    {
        projectile.timeLeft = 2;

        const float DesiredSpeed = 18f;
        const float TurnStrength = 0.1f;

        float distance = projectile.Distance(target.Center);
        float speed = Math.Min(DesiredSpeed, distance);
        Vector2 direction = projectile.DirectionTo(target.Center);
        if (!direction.HasNaNs())
            projectile.velocity = Vector2.Lerp(projectile.velocity, direction * speed, TurnStrength);
        projectile.netUpdate = true;

        projectile.rotation += 0.020943953f + Math.Abs(projectile.velocity.X) * 0.2f;
        if (Main.rand.Next(3) == 0)
        {
            Dust dust = Dust.NewDustDirect(projectile.Center, 0, 0, 43, projectile.velocity.X, projectile.velocity.Y, 254, Color.White, 0.5f);
            Vector2 spread = Main.rand.NextVector2Circular(1f, 1f);
            dust.position = projectile.Center + spread * 10f;
            dust.velocity = spread;
        }
    }

    private static bool IsValidTarget(Player target, Player owner)
    {
        if (!target.active || target.dead || !target.hostile)
            return false;
        if (target.whoAmI == owner.whoAmI)
            return false;
        if (owner.team != 0 && target.team == owner.team)
            return false;
        if (!target.HasBuff(ModContent.BuffType<BitingEmbrace>()))
            return false;
        return true;
    }

    public static void TrySpawnSnowflake(Player attacker, Player target)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (attacker.whoAmI != Main.myPlayer)
            return;

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile p = Main.projectile[i];
            if (p.active && p.type == ProjectileID.CoolWhipProj && p.owner == attacker.whoAmI)
                return;
        }

        Projectile.NewProjectile(
            attacker.GetSource_FromThis(),
            target.Center,
            Vector2.Zero,
            ProjectileID.CoolWhipProj,
            30,
            2f,
            attacker.whoAmI
        );
    }
}