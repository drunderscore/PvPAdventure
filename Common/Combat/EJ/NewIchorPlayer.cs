using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class NewIchorPlayer : ModPlayer
{
    public bool hasDefenseReduction = false;

    public override void ResetEffects()
    {
        hasDefenseReduction = false;
    }

    public override void PostUpdateBuffs()
    {
        // Convert vanilla Ichor to our custom debuff
        if (Player.HasBuff(BuffID.Ichor))
        {
            // Get the remaining time of the vanilla Ichor debuff
            int ichorBuffIndex = Player.FindBuffIndex(BuffID.Ichor);
            if (ichorBuffIndex != -1)
            {
                int remainingTime = Player.buffTime[ichorBuffIndex];

                // Remove vanilla Ichor
                Player.DelBuff(ichorBuffIndex);

                // Add our custom debuff with the same duration
                Player.AddBuff(ModContent.BuffType<NewIchorPlayerDebuff>(), remainingTime);
            }
        }

        // Check if player has our custom debuff
        if (Player.HasBuff(ModContent.BuffType<NewIchorPlayerDebuff>()))
        {
            hasDefenseReduction = true;
        }
    }

    public override void PostUpdateEquips()
    {

        if (hasDefenseReduction)
        {
            // Calculate 33% reduction (rounded down)
            int originalDefense = Player.statDefense;
            int reduction = (int)(originalDefense * 0.33f);
            Player.statDefense -= reduction;
            Player.ichor = true;
        }
    }
}
