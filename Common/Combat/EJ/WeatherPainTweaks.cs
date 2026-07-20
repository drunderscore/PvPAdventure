using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
#nullable enable
namespace PvPAdventure.Common.Combat.EJ;
/// <summary>
/// projectile AI changes for the Weather Pain that allow it to target players in PvP, as well as shooting with homing or without homing for PvP, with logic taken directly from vanilla code
/// </summary>
public class WeatherPainAltUse : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.WeatherPain;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.WeatherPain] = true;
    }

    public override bool AltFunctionUse(Item item, Player player) => true;
}

public class WeatherPainShotRework : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.WeatherPainShot;

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

    private static void ApplyLifecycleAndSound(Projectile projectile)
    {
        if (projectile.soundDelay == 0)
        {
            projectile.soundDelay = -1;
            projectile.localAI[1] = SoundEngine.PlayTrackedSound(SoundID.DD2_BookStaffTwisterLoop, projectile.Center).ToFloat();
        }

        ActiveSound? activeSound = SoundEngine.GetActiveSound(SlotId.FromFloat(projectile.localAI[1]));
        if (activeSound != null)
        {
            activeSound.Position = projectile.Center;
            activeSound.Volume = 1f - Math.Max(projectile.ai[1] - 555f, 0f) / 15f;
        }
        else
        {
            projectile.localAI[1] = SlotId.Invalid.ToFloat();
        }

        projectile.ai[1] += 1f;
        if (projectile.ai[1] > 560f)
            projectile.alpha = (int)MathHelper.Lerp(0f, 250f, (projectile.ai[1] - 560f) / 10f);
        if (projectile.ai[1] >= 570f)
            projectile.Kill();

        const float cutoffFrame = 555f;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile other = Main.projectile[i];
            if (i != projectile.whoAmI && other.active && other.owner == projectile.owner && other.type == projectile.type
                && projectile.timeLeft > other.timeLeft && other.ai[1] < cutoffFrame)
            {
                other.ai[1] = cutoffFrame;
                other.netUpdate = true;
            }
        }
    }

    private static void ApplySpinAndDustTrail(Projectile projectile)
    {
        projectile.rotation = projectile.velocity.X * 0.0125f;

        projectile.frameCounter++;
        if (projectile.frameCounter > 4)
        {
            projectile.frameCounter = 0;
            projectile.frame++;
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.frame = 0;
        }

        if (projectile.timeLeft % 3 == 0)
        {
            int dustIndex = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Cloud, projectile.velocity.X, projectile.velocity.Y, 120, default(Color), 0.5f);
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].fadeIn = 0.9f;
            Main.dust[dustIndex].velocity = Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -2f) + projectile.velocity * 0.75f;

            for (int j = 0; j < 2; j++)
            {
                Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Cloud, projectile.velocity.X, projectile.velocity.Y, 60, default(Color), 0.5f);
                dust.noGravity = true;
                dust.fadeIn = 0.7f;
                dust.velocity = Main.rand.NextVector2Circular(2f, 2f) * 0.2f + new Vector2(0f, -0.4f) + projectile.velocity * 1.5f;
                dust.position -= projectile.velocity * 3f;
            }
        }
    }

    private static void RunPlayerHomingAI(Projectile projectile, Player target)
    {
        ApplyLifecycleAndSound(projectile);

        const float CruiseSpeed = 8f;
        const float TurnStrength = 0.075f;
        const float MinDistance = 25f;

        float distance = projectile.Distance(target.Center);
        float speed = Math.Min(CruiseSpeed, distance);
        Vector2 direction = projectile.DirectionTo(target.Center);
        if (!direction.HasNaNs() && distance >= MinDistance)
            projectile.velocity = Vector2.Lerp(projectile.velocity, direction * speed, TurnStrength);

        ApplySpinAndDustTrail(projectile);
        projectile.netUpdate = true;
    }

    private static void RunStraightFlightAI(Projectile projectile)
    {
        ApplyLifecycleAndSound(projectile);

        if (projectile.velocity != Vector2.Zero)
        {
            Vector2 direction = Vector2.Normalize(projectile.velocity);
            projectile.velocity = Vector2.Lerp(projectile.velocity, direction * 8f, 0.075f);
        }

        ApplySpinAndDustTrail(projectile);
    }
}