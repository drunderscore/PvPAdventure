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
/// Makes the Fairy Queen's magic shots home in on the cursor with the standard shoot style, and adds an alternate shoot style for vanilla homing behavior.
/// </summary>

public class NightglowAltUse : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.FairyQueenMagicItem;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.FairyQueenMagicItem] = true;
    }

    public override bool AltFunctionUse(Item item, Player player) => true;
}


public class AdventureNightglow : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.FairyQueenMagicItemShot;

    public override bool InstancePerEntity => true;

    private bool _cursorHomingMode;
    private int _lockedPlayerTarget = -1;

    public override void SetDefaults(Projectile entity)
    {
        entity.localAI[0] = 0;
    }

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (!projectile.TryGetOwner(out var owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        _cursorHomingMode = owner.altFunctionUse != 2;
        projectile.netUpdate = true;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(_cursorHomingMode);
        binaryWriter.Write((sbyte)_lockedPlayerTarget);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        _cursorHomingMode = bitReader.ReadBit();
        _lockedPlayerTarget = binaryReader.ReadSByte();
    }

    public override void AI(Projectile projectile)
    {
        if (projectile.localAI[0] <= 60)
        {
            projectile.localAI[0]++;
            return;
        }

        if (!projectile.TryGetOwner(out var owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;
        if (owner.itemAnimation <= 0 || owner.HeldItem.type != ItemID.FairyQueenMagicItem)
            return;

        if (_cursorHomingMode)
            RunCursorHoming(projectile);
        else
            RunEnemyHoming(projectile, owner);
    }

    private static void RunCursorHoming(Projectile projectile)
    {
        var cursorPosition = Main.MouseWorld;
        var toCursor = cursorPosition - projectile.Center;
        Steer(projectile, toCursor);
    }

    private void RunEnemyHoming(Projectile projectile, Player owner)
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

        Vector2? targetCenter = _lockedPlayerTarget != -1
            ? Main.player[_lockedPlayerTarget].Center
            : FindClosestNpc(projectile)?.Center;

        if (targetCenter == null)
            return;

        Steer(projectile, targetCenter.Value - projectile.Center);
    }

    private static void Steer(Projectile projectile, Vector2 toTarget)
    {
        const float baseSpeed = 20.0f;
        const float accelerationFactor = 1.5f;
        const float turnStrength = 0.07f;

        var direction = toTarget.SafeNormalize(Vector2.Zero);
        var targetVelocity = direction * baseSpeed * accelerationFactor;
        projectile.velocity = Vector2.Lerp(projectile.velocity, targetVelocity, turnStrength);
        projectile.rotation = projectile.velocity.ToRotation() * MathHelper.PiOver2;
        projectile.netUpdate = true;
    }

    private static NPC? FindClosestNpc(Projectile projectile)
    {
        NPC? best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.CanBeChasedBy(projectile, false))
                continue;

            float distSq = Vector2.DistanceSquared(projectile.Center, npc.Center);
            if (distSq < bestDistSq && Collision.CanHit(projectile.position, projectile.width, projectile.height, npc.position, npc.width, npc.height))
            {
                bestDistSq = distSq;
                best = npc;
            }
        }

        return best;
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
}