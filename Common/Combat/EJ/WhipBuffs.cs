using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Projectiles;

/// <summary>
/// Grants buffs to whip users in PvP when they hit a player.
/// </summary>
public class WhipBuffs : GlobalProjectile
{
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        if (!target.hostile)
            return;
        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player attacker = Main.player[projectile.owner];
        if (attacker == null || !attacker.active || attacker == target || !attacker.hostile)
            return;

        const int BuffDuration = 420;

        switch (projectile.type)
        {
            case ProjectileID.SwordWhip:
                attacker.AddBuff(BuffID.SwordWhipPlayerBuff, BuffDuration);
                break;

            case ProjectileID.ThornWhip:
                attacker.AddBuff(BuffID.ThornWhipPlayerBuff, BuffDuration);
                break;

            case ProjectileID.ScytheWhip:
                attacker.AddBuff(BuffID.ScytheWhipPlayerBuff, BuffDuration);
                break;

            case ProjectileID.CoolWhip:
                attacker.AddBuff(BuffID.CoolWhipPlayerBuff, BuffDuration);
                attacker.GetModPlayer<WhipBuffPlayer>().PendingSnowflakeSpawn = true;
                attacker.GetModPlayer<WhipBuffPlayer>().SnowflakeTarget = target;
                break;
        }
    }
}
public class WhipBuffPlayer : ModPlayer
{
    public bool PendingSnowflakeSpawn = false;
    public Player? SnowflakeTarget = null;

    public override void PostUpdate()
    {
        if (PendingSnowflakeSpawn)
        {
            PendingSnowflakeSpawn = false;
            if (SnowflakeTarget != null)
            {
                PvPSnowflake.TrySpawnSnowflake(Player, SnowflakeTarget);
                SnowflakeTarget = null;
            }
        }
    }
}