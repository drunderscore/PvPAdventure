using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Converts specific vanilla damage reduction sources from additive reduction to independent multiplicative reduction
/// </summary>
internal class PlayerEndurance : ModPlayer
{
    private const float TurtleSetDR = 0.15f;     
    private const float ChlorophyteMeleeSetDR = 0.05f;
    private const float WormScarfDR = 0.17f;
    private const float IceBarrierDR = 0.25f;      
    private const float EnduranceBuffDR = 0.10f;    

    private bool wearingTurtleSet;
    private bool wearingChlorophyteMeleeSet;
    private bool wearingWormScarf;

    public override void ResetEffects()
    {
        wearingTurtleSet = false;
        wearingChlorophyteMeleeSet = false;
        wearingWormScarf = false;
    }

    public override void PostUpdateEquips()
    {
        wearingTurtleSet = Player.armor[0].type == ItemID.TurtleHelmet &&
                            Player.armor[1].type == ItemID.TurtleScaleMail &&
                            Player.armor[2].type == ItemID.TurtleLeggings;


        wearingChlorophyteMeleeSet = Player.armor[0].type == ItemID.ChlorophyteMask &&
                                      Player.armor[1].type == ItemID.ChlorophytePlateMail &&
                                      Player.armor[2].type == ItemID.ChlorophyteGreaves;

        wearingWormScarf = false;
        for (int i = 3; i < 10; i++)
        {
            if (Player.armor[i].type == ItemID.WormScarf)
            {
                wearingWormScarf = true;
                break;
            }
        }

        if (wearingTurtleSet)
            Player.endurance -= TurtleSetDR;

        if (wearingChlorophyteMeleeSet)
            Player.endurance -= ChlorophyteMeleeSetDR;

        if (wearingWormScarf)
            Player.endurance -= WormScarfDR;

        if (Player.HasBuff(BuffID.IceBarrier))
            Player.endurance -= IceBarrierDR;

        if (Player.HasBuff(BuffID.Endurance))
            Player.endurance -= EnduranceBuffDR; 

        if (Player.endurance < 0f)
            Player.endurance = 0f;
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (wearingTurtleSet)
            modifiers.FinalDamage *= 1f - TurtleSetDR;

        if (wearingChlorophyteMeleeSet)
            modifiers.FinalDamage *= 1f - ChlorophyteMeleeSetDR;

        if (wearingWormScarf)
            modifiers.FinalDamage *= 1f - WormScarfDR;

        if (Player.HasBuff(BuffID.IceBarrier))
            modifiers.FinalDamage *= 1f - IceBarrierDR;

        if (Player.HasBuff(BuffID.Endurance))
            modifiers.FinalDamage *= 1f - EnduranceBuffDR;
    }
}