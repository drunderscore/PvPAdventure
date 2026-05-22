using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using PvPAdventure.Core.Config;
/// <summary>
/// Applies PvP knockback multipliers from config to projectile and melee hits.
/// </summary>
namespace PvPAdventure.Common.Combat.EJ;
public class WeaponBasedKnockbackProjectile : GlobalProjectile
{
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        if (!modifiers.PvP)
            return;
        var config = ModContent.GetInstance<ServerConfig>();
        int ownerIndex = projectile.owner;
        if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
        {
            Player attacker = Main.player[ownerIndex];
            Item sourceItem = FindWeaponForProjectile(attacker, projectile.type);
            if (sourceItem != null && !sourceItem.IsAir)
            {
                var itemDef = new ItemDefinition(sourceItem.type);
                if (config.WeaponBalance.Knockback.ItemKnockback.TryGetValue(itemDef, out float itemMult))
                    modifiers.Knockback *= itemMult;
            }
        }
        if (projectile.type != ProjectileID.None)
        {
            var projDef = new ProjectileDefinition(projectile.type);
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
public class WeaponBasedKnockbackItem : GlobalItem
{
    public override void ModifyHitPvp(Item item, Player attacker, Player target, ref Player.HurtModifiers modifiers)
    {
        var config = ModContent.GetInstance<ServerConfig>();
        var itemDef = new ItemDefinition(item.type);
        if (config.WeaponBalance.Knockback.ItemKnockback.TryGetValue(itemDef, out float itemMult))
            modifiers.Knockback *= itemMult;
        modifiers.Knockback *= config.WeaponBalance.Knockback.PvPKnockbackMultiplier;
    }
}