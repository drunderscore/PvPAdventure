using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Makes all summon weapons (not whips) cost no mana and have lower use time.
/// Also forces summon projectiles to spawn at the player instead of the cursor.
/// This is to be more in line with 1.4.5 and to provide quality of life.
/// </summary>
public class SummonWeaponTweaks : GlobalItem
{
    public override void SetDefaults(Item item)
    {
        if (item.DamageType != DamageClass.Summon || item.shoot <= ProjectileID.None)
            return;

        item.mana = 0;
        item.useTime = 10;
        item.useAnimation = 10;
    }

    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (item.DamageType != DamageClass.Summon || item.shoot <= ProjectileID.None)
            return true;

        // Spawn at player center instead of cursor
        Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);

        return false;
    }
}