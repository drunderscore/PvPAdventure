using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
#nullable enable
namespace PvPAdventure.Common.Combat.EJ;
/// <summary>
/// projectile AI changes for the Razorblade Typhoon that allow it to target players in PvP, as well as shooting with homing or without homing for PvP, with logic taken directly from vanilla code
/// </summary>
public class RazorbladeTyphoonAltUse : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.RazorbladeTyphoon;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.RazorbladeTyphoon] = true;
    }

    public override bool AltFunctionUse(Item item, Player player) => true;
}

public class RazorbladeTyphoonRework : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.Typhoon;

    public override bool InstancePerEntity => true;

    private bool _forceNoHoming;
    private int _lockedPlayerTarget = -1;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (!projectile.TryGetOwner(out Player? owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        _forceNoHoming = owner.altFunctionUse != 2;
        projectile.netUpdate = true;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(_forceNoHoming);
        binaryWriter.Write((sbyte)_lockedPlayerTarget);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        _forceNoHoming = bitReader.ReadBit();
        _lockedPlayerTarget = binaryReader.ReadSByte();
    }

    public override bool PreAI(Projectile projectile)
    {
        if (!projectile.TryGetOwner(out Player? owner) || owner is null || !owner.active || owner.dead)
            return true;

        if (owner.whoAmI == Main.myPlayer)
        {
            if (_lockedPlayerTarget == -1)
            {
                Player? justHit = FindHitPlayer(projectile, owner);
                if (justHit != null)
                {
                    _lockedPlayerTarget = justHit.whoAmI;
                    projectile.netUpdate = true;
                }
            }
            else if (!IsValidLockedTarget(Main.player[_lockedPlayerTarget], owner))
            {
                _lockedPlayerTarget = -1;
                projectile.netUpdate = true;
            }
        }

        if (_lockedPlayerTarget != -1)
        {
            RunPlayerHomingAI(projectile, Main.player[_lockedPlayerTarget]);
            return false;
        }

        if (_forceNoHoming)
        {
            RunStraightFlightAI(projectile);
            return false;
        }

        return true;
    }

    private static Player? FindHitPlayer(Projectile projectile, Player owner)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player candidate = Main.player[i];
            if (!IsValidLockedTarget(candidate, owner))
                continue;
            if (projectile.Hitbox.Intersects(candidate.Hitbox))
                return candidate;
        }
        return null;
    }

    private static bool IsValidLockedTarget(Player target, Player owner)
    {
        if (!target.active || target.dead || !target.hostile)
            return false;
        if (target.whoAmI == owner.whoAmI)
            return false;
        if (owner.team != 0 && target.team == owner.team)
            return false;
        return true;
    }
    private static void ApplyVanillaCosmetics(Projectile projectile)
    {
        projectile.localAI[1] += 1f;

        if (projectile.localAI[1] > 10f && Main.rand.Next(3) == 0)
        {
            const int burstCount = 6;
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 spawnOffset = Vector2.Normalize(projectile.velocity) * new Vector2(projectile.width, projectile.height) / 2f;
                spawnOffset = spawnOffset.RotatedBy((i - (burstCount / 2 - 1)) * Math.PI / burstCount, default(Vector2)) + projectile.Center;

                Vector2 spread = ((float)(Main.rand.NextDouble() * 3.1415927410125732) - 1.5707964f).ToRotationVector2() * Main.rand.Next(3, 8);
                int dustIndex = Dust.NewDust(spawnOffset + spread, 0, 0, DustID.RazorbladeTyphoon, spread.X * 2f, spread.Y * 2f, 100, default(Color), 1.4f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].velocity /= 4f;
                Main.dust[dustIndex].velocity -= projectile.velocity;
            }

            projectile.alpha -= 5;
            if (projectile.alpha < 50)
                projectile.alpha = 50;

            projectile.rotation += projectile.velocity.X * 0.1f;
            projectile.frame = (int)(projectile.localAI[1] / 3f) % 3;
            Lighting.AddLight((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16, 0.1f, 0.4f, 0.6f);
        }
    }
    private static void ApplyConstantAcceleration(Projectile projectile)
    {
        float speed = projectile.velocity.Length();
        projectile.velocity.Normalize();
        projectile.velocity *= speed + 0.0025f;
    }

    private static void RunPlayerHomingAI(Projectile projectile, Player target)
    {
        ApplyVanillaCosmetics(projectile);

        Vector2 toTarget = target.Center - projectile.Center;
        float currentAngle = projectile.velocity.ToRotation();
        float targetAngle = toTarget.ToRotation();
        double deltaAngle = targetAngle - currentAngle;
        if (deltaAngle > Math.PI)
            deltaAngle -= Math.PI * 2;
        if (deltaAngle < -Math.PI)
            deltaAngle += Math.PI * 2;

        projectile.velocity = projectile.velocity.RotatedBy(deltaAngle * 0.1, default(Vector2));

        ApplyConstantAcceleration(projectile);
        projectile.netUpdate = true;
    }

    private static void RunStraightFlightAI(Projectile projectile)
    {
        ApplyVanillaCosmetics(projectile);
        ApplyConstantAcceleration(projectile);
    }
}