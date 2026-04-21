using Microsoft.Xna.Framework;
using PvPAdventure.Common.Combat.EJ;
using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Projectiles;

/// <summary>
/// AI changes for the coolwhip snowflake that allow it to target players in PvP, as well as letting it spawn in PvP like how it it spawns in PvE
/// </summary>
public class PvPSnowflake : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.CoolWhipProj;

    public override bool InstancePerEntity => true;

    public override bool PreAI(Projectile projectile)
    {
        projectile.timeLeft = 2;
        return true; 
    }

    public override void PostAI(Projectile projectile)
    {
        if (!projectile.TryGetOwner(out Player owner))
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        if (!owner.HasBuff(BuffID.CoolWhipPlayerBuff))
        {
            projectile.Kill();
            return;
        }

        Player? target = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active || !p.hostile || p.dead)
                continue;
            if (p.whoAmI == owner.whoAmI)
                continue;
            if (owner.team != 0 && p.team == owner.team)
                continue;
            if (!p.HasBuff(ModContent.BuffType<BitingEmbrace>()))
                continue;

            float dist = Vector2.Distance(projectile.Center, p.Center);
            if (dist < bestDist)
            {
                bestDist = dist;
                target = p;
            }
        }
        if (target == null)
            return;

        const float BaseSpeed = 12f;
        const float TurnStrength = 0.08f;
        const float MaxSpeed = 16f;

        Vector2 toTarget = (target.Center - projectile.Center).SafeNormalize(Vector2.Zero);
        projectile.velocity = Vector2.Lerp(projectile.velocity, toTarget * BaseSpeed, TurnStrength);

        if (projectile.velocity.Length() > MaxSpeed)
            projectile.velocity = Vector2.Normalize(projectile.velocity) * MaxSpeed;

        projectile.rotation = projectile.velocity.ToRotation();
        projectile.netUpdate = true;
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