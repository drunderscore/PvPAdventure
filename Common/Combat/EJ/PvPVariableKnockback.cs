using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using PvPAdventure.Core.Config;
/// <summary>
/// Makes the knockback stat affect the amount of knockback players take in PvP, as well as adding a new config option for knockback.
/// </summary>
namespace PvPAdventure.Common.Combat.EJ;

public class WeaponBasedKnockback : ModPlayer
{
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!modifiers.PvP)
            return;
        int attackerIndex = modifiers.DamageSource.SourcePlayerIndex;
        if (attackerIndex < 0 || attackerIndex >= Main.maxPlayers)
            return;
        Player attacker = Main.player[attackerIndex];
        if (!attacker.active || !attacker.hostile || attacker.team == Player.team)
            return;
        Item sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem == null || sourceItem.IsAir)
        {
            if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
            {
                sourceItem = FindWeaponForProjectile(attacker, modifiers.DamageSource.SourceProjectileType);
            }
        }
        var config = ModContent.GetInstance<ServerConfig>();
        if (sourceItem != null && !sourceItem.IsAir)
        {
            float weaponKnockback = attacker.GetWeaponKnockback(sourceItem, sourceItem.knockBack);
            modifiers.Knockback.Base = weaponKnockback;
        }
        if (sourceItem != null && !sourceItem.IsAir)
        {
            var itemDef = new ItemDefinition(sourceItem.type);
            if (config.WeaponBalance.Knockback.ItemKnockback.TryGetValue(itemDef, out float itemMult))
                modifiers.Knockback *= itemMult;
        }
        if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            var projDef = new ProjectileDefinition(modifiers.DamageSource.SourceProjectileType);
            if (config.WeaponBalance.Knockback.ProjectileKnockback.TryGetValue(projDef, out float projMult))
                modifiers.Knockback *= projMult;
        }
        modifiers.Knockback *= config.WeaponBalance.Knockback.PvPKnockbackMultiplier;
    }

    private Item FindWeaponForProjectile(Player attacker, int projType)
    {
        if (attacker.HeldItem != null && !attacker.HeldItem.IsAir && attacker.HeldItem.shoot == projType)
            return attacker.HeldItem;

        for (int i = 0; i < attacker.inventory.Length; i++)
        {
            Item item = attacker.inventory[i];
            if (item != null && !item.IsAir && item.shoot == projType)
                return item;
        }

        return null;
    }
}