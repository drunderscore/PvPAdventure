using PvPAdventure.Common.Game.GameReporters;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>
/// Tracks how many distinct players a single Sniper Rifle projectile hits.
/// Fires <see cref="AchievementReporter.OnSniperDoubleHit"/> the moment the second hit lands.
///
/// Requires <c>InstancePerEntity = true</c> so each projectile has its own hit counter.
/// </summary>
public sealed class SniperMultiHitTracker : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    private int _playersHit;

    // Opt out of unnecessary allocation for every other projectile type.
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.type == ProjectileID.SniperBullet;

    public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
    {
        // Server is authoritative for PvP; avoid double-reporting on clients.
        if (Main.netMode != NetmodeID.Server)
            return;

        _playersHit++;

        if (_playersHit == 2)
        {
            Player shooter = Main.player[projectile.owner];
            if (shooter.active)
                AchievementReporter.OnSniperDoubleHit(shooter);
        }
    }
}
