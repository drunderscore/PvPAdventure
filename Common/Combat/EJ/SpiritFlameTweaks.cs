using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
#nullable enable
namespace PvPAdventure.Common.Combat.EJ;

public class SpiritFlameAltUse : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.SpiritFlame;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.SpiritFlame] = true;
    }

    public override bool AltFunctionUse(Item item, Player player) => true;
}
/// <summary>
/// Makes the Spirit FLame home in on the cursor with the standard shoot style, and adds an alternate shoot style for vanilla homing behavior.
/// </summary>
public class SpiritFlameRework : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ProjectileID.SpiritFlame;

    public override bool InstancePerEntity => true;
    // Vanilla lifetime behavior
    private const float CursorSpeed = 7.2f;
    private const int ReleasedLifetime = 420;
    private const int ExplosionSize = 80;

    private bool _customMode;
    private bool _released;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (!projectile.TryGetOwner(out Player? owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        _customMode = owner.altFunctionUse != 2;
        if (_customMode)
            projectile.tileCollide = true;
        projectile.netUpdate = true;
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(_customMode);
        bitWriter.WriteBit(_released);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        _customMode = bitReader.ReadBit();
        _released = bitReader.ReadBit();
        if (_customMode)
            projectile.tileCollide = true;
    }

    public override bool PreAI(Projectile projectile)
    {
        if (!_customMode)
            return true;

        if (!_released && projectile.TryGetOwner(out Player? owner) && owner != null && owner.whoAmI == Main.myPlayer)
        {
            bool stillActivelyUsing = owner.itemAnimation > 0 && owner.HeldItem.type == ItemID.SpiritFlame;
            if (!stillActivelyUsing)
            {
                _released = true;
                projectile.timeLeft = ReleasedLifetime;
                projectile.netUpdate = true;
            }
        }

        if (_released)
        {
            RunCursorHoming(projectile);
            return false;
        }

        return true;
    }

    public override void PostAI(Projectile projectile)
    {
        if (_customMode && !_released)
            projectile.velocity = Vector2.Zero;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        if (_customMode)
            projectile.Kill();
        return true;
    }

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (_customMode)
            projectile.Kill();
    }

    public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
    {
        if (_customMode)
            projectile.Kill();
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (!_customMode)
            return;

        SpawnExplosionEffects(projectile);

        if (!projectile.TryGetOwner(out Player? owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        ApplyExplosionDamage(projectile);
    }

    private static void RunCursorHoming(Projectile projectile)
    {
        if (!projectile.TryGetOwner(out Player? owner) || owner is null)
            return;
        if (owner.whoAmI != Main.myPlayer)
            return;

        projectile.alpha = 0;

        Vector2 direction = (Main.MouseWorld - projectile.Center).SafeNormalize(Vector2.Zero);
        projectile.velocity = direction * CursorSpeed;
        projectile.netUpdate = true;
    }

    private static void SpawnExplosionEffects(Projectile projectile)
    {
        Vector2 center = projectile.Center;

        SoundEngine.PlaySound(SoundID.Item14, projectile.position);

        const int ringCount = 15;
        const int totalCount = ringCount + 15;
        for (int i = 0; i < totalCount; i++)
        {
            int dustIndex = Dust.NewDust(projectile.position, projectile.width, projectile.height, 27, 0f, 0f, 0, default(Color), 2f + Main.rand.NextFloat() * 0.5f);
            Main.dust[dustIndex].noGravity = true;

            if (i < ringCount)
            {
                float angle = (i + 1) / (float)ringCount * 6.2831855f;
                Main.dust[dustIndex].fadeIn = 1.5f + Main.rand.NextFloat() * 0.5f;
                Main.dust[dustIndex].position = center;
                Main.dust[dustIndex].velocity = Vector2.UnitY.RotatedBy(angle) * (5f + Main.rand.NextFloat() * 1.5f);
            }
            else
            {
                Main.dust[dustIndex].position = center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * Main.rand.NextFloat() * projectile.width / 3f;
                Main.dust[dustIndex].fadeIn = 0.5f + Main.rand.NextFloat() * 0.5f;
                Main.dust[dustIndex].velocity *= 2f;
            }
        }

        for (int i = 0; i < 10; i++)
        {
            int dustIndex = Dust.NewDust(projectile.position, projectile.width, projectile.height, 31, 0f, 0f, 0, default(Color), 1.5f);
            Main.dust[dustIndex].position = center + Vector2.UnitX.RotatedByRandom(3.1415927410125732).RotatedBy(projectile.velocity.ToRotation()) * projectile.width / 3f;
            Main.dust[dustIndex].fadeIn = 0.5f + Main.rand.NextFloat() * 0.5f;
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].velocity *= 1.5f;
        }
    }

    private static void ApplyExplosionDamage(Projectile projectile)
    {
        Vector2 oldCenter = projectile.Center;
        projectile.position = oldCenter;
        projectile.width = projectile.height = ExplosionSize;
        projectile.Center = oldCenter;

        projectile.Damage();
    }
}