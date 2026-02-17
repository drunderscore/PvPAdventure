using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;
/// <summary>
/// Removes the "flat%" damage nerf from Spectre armor and replaces it with a multiplicative nerf to all magic damage projectiles while wearing it, so that spectre armor is less effected by defense.
/// </summary>
public class SpectreHealRework : ModPlayer
{
    public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
    {
        if (!Player.ghostHeal || item.DamageType != DamageClass.Magic)
            return;

        damage += 0.40f;
    }
}

public class SpectreHealProjectile : GlobalProjectile
{
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player owner = Main.player[projectile.owner];

        if (!owner.ghostHeal || projectile.DamageType != DamageClass.Magic)
            return;

        modifiers.FinalDamage *= 0.60f;
    }

    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player owner = Main.player[projectile.owner];

        if (!owner.ghostHeal || projectile.DamageType != DamageClass.Magic)
            return;

        modifiers.FinalDamage *= 0.60f;
    }
}
