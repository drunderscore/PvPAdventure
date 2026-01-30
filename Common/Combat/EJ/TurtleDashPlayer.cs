using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class TurtleDashPlayer : ModPlayer
{
    private bool isWearingFullTurtleArmor = false;
    private int dashingTimer = 0;

    // Detect if player is in a dash state
    public bool IsInADashState => (Player.dashDelay == -1 || dashingTimer > 0) && Player.grapCount <= 0;

    public override void PostUpdateEquips()
    {

        isWearingFullTurtleArmor = Player.armor[0].type == ItemID.TurtleHelmet &&
                                   Player.armor[1].type == ItemID.TurtleScaleMail &&
                                   Player.armor[2].type == ItemID.TurtleLeggings;

        if (isWearingFullTurtleArmor)
        {
            Player.AddBuff(ModContent.BuffType<BROISACHOJ>(), 1 * 60 * 60);
        }
    }

    public override void PostUpdate()
    {

        if (Player.dashDelay == -1)
        {
            dashingTimer = 10;
        }
        else if (dashingTimer > 0)
        {
            dashingTimer--;
        }

        // Apply dash speed reduction if player has the debuff and is dashing
        if (Player.HasBuff(ModContent.BuffType<BROISACHOJ>()) && IsInADashState)
        {
            float dashSpeedReduction = Player.velocity.X * 0.05f;
            Player.velocity.X -= dashSpeedReduction;
        }
        if (Player.HasBuff(BuffID.BabyEater) && IsInADashState)
        {
            float dashSpeedReduction = Player.velocity.X * -0.03f;
            Player.velocity.X -= dashSpeedReduction;
            //Dont think I didnt notice this.
        }
        //thanks mr fargo
    }

}