using Microsoft.Xna.Framework;
using PvPAdventure.Content.Buffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;
/// <summary>
/// - When a player gets hit by another player's whip in PvP, they get a debuff that emulates the "summon tag" debuffs in PvE
/// - Instead of whips that add critical strike chance to summons adding critical strike chance against players, those whips will grant a % damage increase instead.
/// - All debuffs are based off of the helper methods
/// - We need to know who applied the debuff, because we don't want the person who hit them with the whip to benefit off of the Tag
/// - We spawn different dust in a different pattern on hit so players know when someone is tagged, and what whip they were tagged by
/// - Hellhex is a special debuff, because instead of making players take more damage, it makes the next hit create an explosion that does 2.75X the damage of the original hit
/// </summary>

// Check if a player has Tiki Armor equipped, because it increases debuff duration
public class TikiArmorPlayer : ModPlayer
{
    public bool hasTikiSet = false;

    public override void PostUpdateEquips()
    {
        if (Player.armor[0].type == ItemID.TikiMask &&
            Player.armor[1].type == ItemID.TikiShirt &&
            Player.armor[2].type == ItemID.TikiPants)
        {
            hasTikiSet = true;
        }
        else
        {
            hasTikiSet = false;
        }
    }
}
public abstract class WhipDebuffPlayer : ModPlayer
{
    public int applierIndex = -1;

    protected abstract int WhipProjectileID { get; }
    protected abstract int DebuffType { get; }
    protected abstract int BaseDuration { get; }
    protected abstract int FlatDamageBonus { get; }
    protected virtual float PercentDamageBonus => 0f;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == WhipProjectileID)
        {
            int duration = BaseDuration;
            int newApplierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    newApplierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            applierIndex = newApplierIndex;
            Player.AddBuff(DebuffType, duration);
            OnDebuffApplied(info, duration);
        }
    }

    protected virtual void OnDebuffApplied(Player.HurtInfo info, int duration) { }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff(DebuffType) && applierIndex >= 0 && applierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[applierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(DebuffType);
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                OnApplierRemoved();
                applierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff(DebuffType))
        {
            UpdateVisualEffects();
        }
        else
        {
            applierIndex = -1;
            OnDebuffExpired();
        }
    }

    protected virtual void OnApplierRemoved() { }
    protected virtual void OnDebuffExpired() { }
    protected abstract void UpdateVisualEffects();

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff(DebuffType))
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == applierIndex;
            bool isSummon = IsSummonOrWhipDamage(ref modifiers);

            if (!isSummon && !isDebuffApplier)
            {
                if (FlatDamageBonus > 0)
                {
                    modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                        info.Damage += FlatDamageBonus;
                    };
                }
                if (PercentDamageBonus > 0f)
                {
                    modifiers.FinalDamage *= (1f + PercentDamageBonus);
                }
            }
        }
    }

    protected bool IsSummonOrWhipDamage(ref Player.HurtModifiers modifiers)
    {
        if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && proj.CountsAsClass(DamageClass.SummonMeleeSpeed))
            {
                return true;
            }
        }
        else if (modifiers.DamageSource.SourceProjectileType > 0)
        {
            int projType = modifiers.DamageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }
        return false;
    }

    protected void SpawnCircularDust(int dustID, Color color, float baseDistance, float rotationSpeed, int count = 4)
    {
        float pulseTime = Main.GameUpdateCount % 60f / 60f;
        float pulseScale = 1f + (float)Math.Sin(pulseTime * MathHelper.TwoPi) * 0.2f;

        for (int i = 0; i < count; i++)
        {
            float angle = (MathHelper.TwoPi / count) * i + (Main.GameUpdateCount * rotationSpeed);
            float distance = baseDistance * pulseScale;

            Vector2 offset = new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );
            Vector2 dustPosition = Player.Center + offset;

            Dust dust = Dust.NewDustPerfect(dustPosition, dustID, Vector2.Zero, 100, color, 1.5f);
            dust.noGravity = true;
            dust.fadeIn = 1f;
            dust.noLight = false;
        }
    }

    protected void SpawnLineDust(Color color, int count = 2)
    {
        if (Main.rand.NextBool(3))
        {
            for (int i = 0; i < count; i++)
            {
                float lineAngle = i * MathHelper.PiOver2;
                float lineDistance = Main.rand.NextFloat(10f, 40f);

                Vector2 lineOffset = new Vector2(
                    (float)Math.Cos(lineAngle) * lineDistance,
                    (float)Math.Sin(lineAngle) * lineDistance
                );
                Vector2 dustPos = Player.Center + lineOffset;

                Dust lineDust = Dust.NewDustPerfect(dustPos, DustID.Torch, Vector2.Zero, 100, color, 1f);
                lineDust.noGravity = true;
                lineDust.fadeIn = 0.5f;
            }
        }
    }
}

public class BitingEmbracePlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.CoolWhip;
    protected override int DebuffType => ModContent.BuffType<BitingEmbrace>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 7;

    protected override void OnDebuffApplied(Player.HurtInfo info, int duration)
    {
        Player.AddBuff(BuffID.Frostburn2, duration);
    }

    protected override void OnApplierRemoved()
    {
        Player.ClearBuff(BuffID.Frostburn2);
    }

    protected override void UpdateVisualEffects()
    {
        SpawnCircularDust(DustID.IceTorch, Color.Teal, 21f, 0.07f);
        SpawnLineDust(Color.Teal);
    }
}

public class PressurePointsPlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.ThornWhip;
    protected override int DebuffType => ModContent.BuffType<PressurePoints>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 6;

    protected override void UpdateVisualEffects()
    {
        SpawnCircularDust(DustID.CursedTorch, Color.LimeGreen, 12f, 0.05f);
        SpawnLineDust(Color.LimeGreen);
    }
}

public class BrittleBonesPlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.BoneWhip;
    protected override int DebuffType => ModContent.BuffType<BrittleBones>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 7;

    protected override void UpdateVisualEffects()
    {
        SpawnCircularDust(DustID.BoneTorch, Color.DarkGray, 19f, 0.06f);
        SpawnLineDust(Color.DarkGray);
    }
}

public class MarkedPlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.SwordWhip;
    protected override int DebuffType => ModContent.BuffType<Marked>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 9;

    protected override void UpdateVisualEffects()
    {
        SpawnCircularDust(DustID.Blood, Color.Red, 26f, 0.08f);
        SpawnLineDust(Color.DarkRed);
    }
}

public class AnathemaPlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.RainbowWhip;
    protected override int DebuffType => ModContent.BuffType<Anathema>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 20;
    protected override float PercentDamageBonus => 0.1f;

    protected override void UpdateVisualEffects()
    {
        for (int i = 0; i < 3; i++)
        {
            float distance = Main.rand.NextFloat(40f, 80f);
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);

            Vector2 spawnOffset = new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );
            Vector2 dustPosition = Player.Center + spawnOffset;

            Vector2 towardPlayer = Player.Center - dustPosition;
            towardPlayer.Normalize();
            Vector2 dustVelocity = towardPlayer * Main.rand.NextFloat(2f, 4f);

            int dustType = Main.rand.NextBool() ? DustID.PlatinumCoin : DustID.Smoke;
            Color dustColor = Main.rand.NextBool() ? Color.White : Color.Black;

            Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, dustColor, Main.rand.NextFloat(0.6f, 1.2f));
            dust.noGravity = true;
            dust.fadeIn = 0.8f;
        }
    }
}

public class ShatteredArmorPlayer : WhipDebuffPlayer
{
    protected override int WhipProjectileID => ProjectileID.MaceWhip;
    protected override int DebuffType => ModContent.BuffType<ShatteredArmor>();
    protected override int BaseDuration => 300;
    protected override int FlatDamageBonus => 8;
    protected override float PercentDamageBonus => 0.12f;

    protected override void UpdateVisualEffects()
    {
        for (int i = 0; i < 1; i++)
        {
            int dustType = DustID.BatScepter;
            Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
            Vector2 dustVelocity = Player.velocity * 0.3f + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));

            Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, Color.Black, Main.rand.NextFloat(0.8f, 1.5f));
            dust.noGravity = Main.rand.NextBool(2);
            dust.fadeIn = 1.2f;
        }
    }
}

//FIXME: Hellhex is likely causing a bug with the kill display on the scoreboard. It could be a conflict with the PreKill section.
public class HellhexPlayer : WhipDebuffPlayer
{
    public bool hellhexTriggered = false;
    private bool explosionSpawned = false;

    protected override int WhipProjectileID => ProjectileID.FireWhip;
    protected override int DebuffType => ModContent.BuffType<Hellhex>();
    protected override int BaseDuration => 450;
    protected override int FlatDamageBonus => 0;

    private bool IsSummonOrWhipDamage(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[info.DamageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && proj.CountsAsClass(DamageClass.SummonMeleeSpeed))
            {
                return true;
            }
        }

        if (info.DamageSource.SourceProjectileType > 0)
        {
            int projType = info.DamageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSummonOrWhipDeath(PlayerDeathReason damageSource)
    {
        if (damageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[damageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && proj.CountsAsClass(DamageClass.SummonMeleeSpeed))
            {
                return true;
            }
        }

        if (damageSource.SourceProjectileType > 0)
        {
            int projType = damageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }

        return false;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {
        if (Player.HasBuff(DebuffType))
        {
            bool isSummon = IsSummonOrWhipDeath(damageSource);
            bool isDebuffApplier = damageSource.SourcePlayerIndex == applierIndex;

            if (!isSummon && !isDebuffApplier && damage >= 30 && !explosionSpawned)
            {
                explosionSpawned = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int owner = applierIndex >= 0 && applierIndex < Main.maxPlayers ? applierIndex : -1;
                    int explosionDamage = (int)(damage * 2.75f);

                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Death(),
                        Player.Center,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner,
                        0f,
                        0f
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                        }
                    }
                }
            }
        }

        return true;
    }

    protected override void OnDebuffApplied(Player.HurtInfo info, int duration)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)0);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)applierIndex);
            packet.Send();
        }
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        base.PostHurt(info);

        if (Player.HasBuff(DebuffType) && !explosionSpawned)
        {
            bool isSummon = IsSummonOrWhipDamage(info);
            bool isDebuffApplier = info.DamageSource.SourcePlayerIndex == applierIndex;

            if (!isSummon && !isDebuffApplier && info.Damage >= 30)
            {
                explosionSpawned = true;

                int buffIndex = Player.FindBuffIndex(DebuffType);
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }

                Vector2 spawnPos = Player.Center;
                float scale = info.Damage / 100f;
                int owner = applierIndex >= 0 && applierIndex < Main.maxPlayers ? applierIndex : -1;
                int explosionDamage = (int)(info.Damage * 2.75f);

                if (Main.netMode == NetmodeID.SinglePlayer ||
                    (info.DamageSource.SourcePlayerIndex >= 0 && Main.myPlayer == info.DamageSource.SourcePlayerIndex))
                {
                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Buff(buffIndex),
                        spawnPos,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].scale = scale;
                    }

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)1);
                        packet.Write((byte)Player.whoAmI);
                        packet.Write(spawnPos.X);
                        packet.Write(spawnPos.Y);
                        packet.Write(explosionDamage);
                        packet.Write(scale);
                        packet.Write((sbyte)owner);
                        packet.Send();
                    }
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Buff(buffIndex),
                        spawnPos,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].scale = scale;
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                    }
                }
            }
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        base.ModifyHurt(ref modifiers);

        if (Player.HasBuff(DebuffType))
        {
            bool isSummon = IsSummonOrWhipDamage(ref modifiers);

            if (!isSummon)
            {
                hellhexTriggered = true;
            }
        }
    }

    protected override void OnApplierRemoved()
    {
        hellhexTriggered = false;
        explosionSpawned = false;
    }

    protected override void OnDebuffExpired()
    {
        hellhexTriggered = false;
        explosionSpawned = false;
    }

    protected override void UpdateVisualEffects()
    {
        for (int i = 0; i < 2; i++)
        {
            int dustType = DustID.Torch;
            Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
            Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-2f, 0f));

            Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, default(Color), Main.rand.NextFloat(1f, 2f));
            dust.noGravity = true;
            dust.fadeIn = 1.3f;
        }

        if (Main.rand.NextBool(2))
        {
            int dustType = DustID.Smoke;
            Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
            Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, 0f));

            Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.5f));
            dust.noGravity = true;
        }
    }
}
