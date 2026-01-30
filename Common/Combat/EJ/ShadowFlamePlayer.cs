using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class ShadowFlamePlayer : ModPlayer
{
    public override void PostHurt(Player.HurtInfo info)
    {
        int shadowflameDuration = 0;

        if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlameArrow)
        {
            int maxDuration = 2 * 60;
            float damageRatio = Math.Min(info.Damage / 30f, 1f);
            shadowflameDuration = (int)(maxDuration * damageRatio);
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlame)
        {
            shadowflameDuration = 60 * 10;
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlameKnife)
        {
            int maxDuration = 60 * 2;
            float damageRatio = Math.Min(info.Damage / 25f, 1f);
            shadowflameDuration = (int)(maxDuration * damageRatio);
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.DarkLance)
        {
            shadowflameDuration = 66 * 3; // 3.33 seconds
        }

        if (shadowflameDuration > 0)
        {
            Player.AddBuff(BuffID.ShadowFlame, shadowflameDuration);
        }
    }

    public override void UpdateBadLifeRegen()
    {
        if (Player.HasBuff(BuffID.ShadowFlame))
        {
            if (Player.lifeRegen > 0)
            {
                Player.lifeRegen = 0;
            }
            Player.lifeRegenTime = 0;

            Player.lifeRegen -= 20; // 10 dps
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff(BuffID.ShadowFlame))
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f));

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, DustID.Shadowflame, dustVelocity.X, dustVelocity.Y, 100, default(Color), Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
                dust.fadeIn = 1.2f;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 smokePos = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Dust smoke = Dust.NewDustDirect(smokePos, 0, 0, DustID.Smoke, 0, -1f, 100, Color.Purple, 0.8f);
                smoke.noGravity = true;
            }
        }
    }
}
