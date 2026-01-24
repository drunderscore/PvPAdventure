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

        // Apply Spectre Hood armor penetration bonus for magic damage.
        // config.Other.SpectreHealing.HealerArmorPenetration in [0..1].
        if (isMagicDamage && attacker.ghostHeal)
        {
            float ap = config.Other.SpectreHealing.HealerArmorPenetration;

            if (ap > 0f)
            {
                modifiers.ScalingArmorPenetration += ap;
            }
        }
    }
}
