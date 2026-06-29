using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>Tracks server-authoritative deaths while touching or recently touching lava.</summary>
public class LavaDeathTracker : ModPlayer
{
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Player.lavaWet || Player.lavaTime > 0)
            MatchStatsPlayer.RecordServerStat(Player, MatchStatKey.LavaDeaths);
    }
}
