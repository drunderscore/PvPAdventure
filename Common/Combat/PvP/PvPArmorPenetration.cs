using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Combat.PvP;

internal class PvPArmorPenetration : ModPlayer
{
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!modifiers.PvP)
            return;
        int attackerIndex = modifiers.DamageSource.SourcePlayerIndex;
        if ((uint)attackerIndex >= (uint)Main.maxPlayers)
            return;
        Player attacker = Main.player[attackerIndex];
        if (attacker == null || !attacker.active)
            return;
        var config = ModContent.GetInstance<ServerConfig>();

        // Flat armor penetration adjustments for specific weapons (unchanged behavior).
        Item sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem != null && !sourceItem.IsAir)
        {
            if (sourceItem.type == ItemID.Flamethrower)
                modifiers.ArmorPenetration += 15;
            if (sourceItem.type == ItemID.CrystalVileShard)
                modifiers.ArmorPenetration += 10;
            if (sourceItem.type == ItemID.NettleBurst)
                modifiers.ArmorPenetration += 10;
        }

        // Determine whether this hit is magic damage.
        bool isMagicDamage = false;
        if (sourceItem != null && !sourceItem.IsAir && sourceItem.CountsAsClass(DamageClass.Magic))
        {
            isMagicDamage = true;
        }
        else if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            int projIndex = modifiers.DamageSource.SourceProjectileLocalIndex;
            if ((uint)projIndex < (uint)Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                if (proj != null && proj.active && proj.CountsAsClass(DamageClass.Magic))
                    isMagicDamage = true;
            }
        }

        var apConfig = config.WeaponBalance.ArmorPenetration;
        float baseArmorPen = 0f;
        if (sourceItem != null && !sourceItem.IsAir)
        {
            var itemDef = new ItemDefinition(sourceItem.type);
            if (apConfig.ItemAP.TryGetValue(itemDef, out var armorPen))
                baseArmorPen = Math.Clamp(armorPen, 0f, 1f);
        }
        if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            var projDef = new ProjectileDefinition(modifiers.DamageSource.SourceProjectileType);
            if (apConfig.ProjectileAP.TryGetValue(projDef, out var armorPen))
                baseArmorPen = Math.Clamp(armorPen, 0f, 1f);
        }

        // Apply Spectre Hood armor penetration bonus for magic damage.
        float finalArmorPen = baseArmorPen;
        if (isMagicDamage && attacker.ghostHeal)
        {
            float ap = config.Other.SpectreHealing.HealerArmorPenetration;
            if (ap > 0f)
                finalArmorPen = baseArmorPen + (1f - baseArmorPen) * ap;
        }

        if (finalArmorPen > 0f)
            modifiers.ScalingArmorPenetration += finalArmorPen;
    }
}