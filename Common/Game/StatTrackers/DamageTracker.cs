using Terraria;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>
/// Keeps track of PvP damage dealt, damage taken, and weapon damage during a match.
/// </summary>
internal static class DamageTracker
{
    public static void RecordPostHurt(Player victim, Player.HurtInfo info)
    {
        if (victim == null || !info.PvP || info.DamageSource.SourcePlayerIndex == -1)
            return;

        Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
        MatchStatsPlayer.RecordServerDamage(attacker, victim, info);
    }
}
