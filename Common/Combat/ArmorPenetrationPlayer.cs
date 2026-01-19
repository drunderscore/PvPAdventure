using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

internal class ArmorPenetrationPlayer : ModPlayer
{
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!modifiers.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
        var combatConfig = adventureConfig.Combat;

        // Track base armor penetration
        float baseArmorPen = 0f;
        bool isMagicDamage = false;

        var sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem != null && !sourceItem.IsAir)
        {
            if (sourceItem.type == ItemID.Flamethrower)
            {
                modifiers.ArmorPenetration += 15;
            }
            if (sourceItem.type == ItemID.CrystalVileShard)
            {
                modifiers.ArmorPenetration += 10;
            }
            if (sourceItem.type == ItemID.NettleBurst)
            {
                modifiers.ArmorPenetration += 10;
            }

            // Check if this is a magic weapon
            if (sourceItem.CountsAsClass(DamageClass.Magic))
                isMagicDamage = true;
        }

        if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            // Check if the projectile is magic damage
            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && proj.CountsAsClass(DamageClass.Magic))
                    isMagicDamage = true;
            }
        }

        // Apply Spectre Hood armor penetration bonus for magic damage
        var sourcePlayer = Main.player[modifiers.DamageSource.SourcePlayerIndex];
        float finalArmorPen = baseArmorPen;

        if (isMagicDamage && sourcePlayer.ghostHeal)
        {
            // Use GhostHealMultiplierWearers as a scaling penetration factor
            float bonus = combatConfig.GhostHealMultiplierWearers;
            finalArmorPen = baseArmorPen + (1f - baseArmorPen) * bonus;
        }

        if (finalArmorPen > 0f)
        {
            modifiers.ScalingArmorPenetration += finalArmorPen;
        }
    }
}
