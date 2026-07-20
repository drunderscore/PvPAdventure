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
/// projectile AI changes for the Possessed Hatchet that allow shooting with homing or without homing for PvP, with logic taken directly from vanilla code
/// </summary>
public class PossessedHatchetAltUse : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.PossessedHatchet;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.PossessedHatchet] = true;
    }

    public override bool AltFunctionUse(Item item, Player player) => true;
}

public class PossessedHatchetRework : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.PossessedHatchet;

    public override bool InstancePerEntity => true;

    private bool _forceExtendedRange;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (!projectile.TryGetOwner(out Player? owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        _forceExtendedRange = owner.altFunctionUse != 2;
        projectile.netUpdate = true;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(_forceExtendedRange);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        _forceExtendedRange = bitReader.ReadBit();
    }

    public override bool PreAI(Projectile projectile)
    {
        if (_forceExtendedRange && projectile.ai[0] == 0f)
        {
            RunExtendedRangeAI(projectile);
            return false;
        }

        return true;
    }
    private const float MaxOutboundFrames = 90f; // change this to change how far it fires

    private static void RunExtendedRangeAI(Projectile projectile)
    {
        projectile.ai[1] += 1f;

        if (Main.rand.Next(2) == 0)
        {
            int dustIndex = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Enchanted_Gold, 0f, 0f, 255, default(Color), 0.75f);
            Main.dust[dustIndex].velocity *= 0.1f;
            Main.dust[dustIndex].noGravity = true;
        }

        if (projectile.velocity.X > 0f)
            projectile.spriteDirection = 1;
        else if (projectile.velocity.X < 0f)
            projectile.spriteDirection = -1;

        projectile.rotation += 0.3f * projectile.spriteDirection;

        float targetX = projectile.position.X + projectile.width / 2f + projectile.velocity.X * 100f;
        float targetY = projectile.position.Y + projectile.height / 2f + projectile.velocity.Y * 100f;

        if (projectile.ai[1] >= MaxOutboundFrames)
        {
            projectile.ai[0] = 1f;
            projectile.ai[1] = 0f;
            projectile.netUpdate = true;
        }

        ApplySteering(projectile, targetX, targetY);
    }
    private static void ApplySteering(Projectile projectile, float targetX, float targetY)
    {
        const float desiredSpeed = 12f;
        const float accel = 0.25f;

        Vector2 center = new Vector2(projectile.position.X + projectile.width * 0.5f, projectile.position.Y + projectile.height * 0.5f);
        float dx = targetX - center.X;
        float dy = targetY - center.Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
        float scale = desiredSpeed / dist;
        dx *= scale;
        dy *= scale;

        if (projectile.velocity.X < dx)
        {
            projectile.velocity.X += accel;
            if (projectile.velocity.X < 0f && dx > 0f)
                projectile.velocity.X += accel * 2f;
        }
        else if (projectile.velocity.X > dx)
        {
            projectile.velocity.X -= accel;
            if (projectile.velocity.X > 0f && dx < 0f)
                projectile.velocity.X -= accel * 2f;
        }

        if (projectile.velocity.Y < dy)
        {
            projectile.velocity.Y += accel;
            if (projectile.velocity.Y < 0f && dy > 0f)
                projectile.velocity.Y += accel * 2f;
        }
        else if (projectile.velocity.Y > dy)
        {
            projectile.velocity.Y -= accel;
            if (projectile.velocity.Y > 0f && dy < 0f)
                projectile.velocity.Y -= accel * 2f;
        }
    }
}