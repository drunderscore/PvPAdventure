using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>Tracks each time the local player loses honey during a match.</summary>
public class HoneyLossTracker : ModPlayer
{
    private bool hadHoney;

    public override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        bool hasHoney = Player.honeyWet || Player.HasBuff(BuffID.Honey);

        if (hadHoney && !hasHoney)
            MatchStatsPlayer.RecordLocalStat(MatchStatKey.LostHoney);

        hadHoney = hasHoney;
    }
}
