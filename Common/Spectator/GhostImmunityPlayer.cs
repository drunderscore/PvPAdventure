using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

internal sealed class GhostImmunityPlayer : ModPlayer
{
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        return Player.ghost;
    }

    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
    {
        return !Player.ghost;
    }

    public override bool CanBeHitByProjectile(Projectile proj)
    {
        return !Player.ghost;
    }

    public override bool CanHitPvp(Item item, Player target)
    {
        return target?.ghost != true;
    }

    public override bool CanHitPvpWithProj(Projectile proj, Player target)
    {
        return target?.ghost != true;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (!Player.ghost)
            return true;

        Player.statLife = Math.Max(1, Player.statLifeMax2);
        playSound = false;
        genDust = false;
        return false;
    }
}
