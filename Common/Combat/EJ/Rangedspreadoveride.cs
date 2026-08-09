using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Replaces the way that several vanilla items shoot in order to make their spread and random projectile counts more consistent
/// </summary>
public class RangedSpreadOverride : GlobalItem
{
    //Recoil pattern sequence
    private static readonly float[] DefaultPattern =
        { 0f, 0.5f, -0.5f, 1f, -1f, 0.5f, -0.5f, 0f };
    // first number is how many shots, the second number is the angle of the spread
    private static Dictionary<int, (int Count, float SpreadDegrees)>
        MultiShotRegistry = new()
    {
        { ItemID.Boomstick,           (4,  16f) },
        { ItemID.Shotgun,             (5,  16f) },
        { ItemID.TacticalShotgun,     (6,  16f) },
        { ItemID.QuadBarrelShotgun,   (8,  26f) },
        { ItemID.PiranhaGun,          (3,  20f) },
        { ItemID.Gatligator,          (1,   0f) }, 
        { ItemID.Uzi,                 (1,   0f) },
        { ItemID.Megashark,           (1,   0f) },
        { ItemID.Stynger,             (1,   0f) },
        { ItemID.PewMaticHorn,        (1,   0f) },
        { ItemID.ChlorophyteShotbow,  (3,   5f) },
        { ItemID.BubbleGun,           (3,  10f) },
        { ItemID.PoisonStaff,         (4,  15f) },
        { ItemID.LaserMachinegun,     (1,   0f) }, // TODO: Fix with IL edit
        { ItemID.VenomStaff,          (6,  15f) },
        { ItemID.LeafBlower,          (1,   0f) }, // TODO: Fix with IL edit
        { ItemID.CrystalStorm,        (1,   0f) },
    };

    //Recoil pattern items, for now just Gatligator
    private static Dictionary<int, float> RecoilRegistry = new()
    {
        { ItemID.Gatligator, 4f  },
        { ItemID.ChainGun,   0f  },
    };

    public override void ModifyShootStats(
        Item item, Player player,
        ref Vector2 position, ref Vector2 velocity,
        ref int type, ref int damage, ref float knockback)
    {
        if (!IsRangedWeapon(item))
            return;

        float speed = velocity.Length();
        if (speed <= 0f)
            return;

        //Remove all vanilla spread
        Vector2 aimDirection = (Main.MouseWorld - position).SafeNormalize(Vector2.UnitX);
        velocity = aimDirection * speed;
        if (!RecoilRegistry.TryGetValue(item.type, out float maxDegrees))
            return;

        var patternPlayer = player.GetModPlayer<RangedPatternPlayer>();
        float normalised = patternPlayer.ConsumeOffset(item.type, DefaultPattern);
        float angleOffset = normalised * MathHelper.ToRadians(maxDegrees);
        velocity = velocity.RotatedBy(angleOffset);
    }

    public override bool Shoot(
        Item item, Player player,
        EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity,
        int type, int damage, float knockback)
    {
        float speed = velocity.Length();
        float baseAngle = velocity.ToRotation();

        // Onyx blaster needs its special extra projectile
        if (item.type == ItemID.OnyxBlaster)
        {
            const int count = 4;
            const float spreadDeg = 9f;
            float spreadRad = MathHelper.ToRadians(spreadDeg);
            float halfSpread = spreadRad * 0.5f;
            float angleStep = spreadRad / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle - halfSpread) + angleStep * i;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
                Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
            }

            Vector2 orbVel = velocity.SafeNormalize(Vector2.UnitX) * (speed * 1.3f);
            Projectile.NewProjectile(source, position, orbVel, ProjectileID.BlackBolt, damage * 2, knockback, player.whoAmI);
            return false;
        }

        // Daedalus shoots arrows from 38 blocks above the player
        if (item.type == ItemID.DaedalusStormbow)
        {
            const int count = 3;
            const float xSpacing = 160f;  // pixels between arrows
            float startX = Main.MouseWorld.X - xSpacing * (count - 1) / 2f;
            float spawnY = player.Center.Y - (38 * 16f); // 38 blocks x 16 pixels per block

            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPos = new Vector2(startX + xSpacing * i, spawnY);
                Vector2 arrowVel = (Main.MouseWorld - spawnPos).SafeNormalize(Vector2.UnitY) * speed;
                Projectile.NewProjectile(source, spawnPos, arrowVel, type, damage, knockback, player.whoAmI);
            }

            return false;
        }
        if (!MultiShotRegistry.TryGetValue(item.type, out var cfg))
            return true;

        float spreadRadG = MathHelper.ToRadians(cfg.SpreadDegrees);
        float halfSpreadG = spreadRadG * 0.5f;
        float angleStepG = cfg.Count > 1 ? spreadRadG / (cfg.Count - 1) : 0f;

        for (int i = 0; i < cfg.Count; i++)
        {
            float angle = (baseAngle - halfSpreadG) + angleStepG * i;
            Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
            Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
        }

        return false;
    }


    private static bool IsRangedWeapon(Item item)
        => item.CountsAsClass(DamageClass.Ranged) && item.useAmmo > 0;
}