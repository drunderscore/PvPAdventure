using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class WhipRangePlayer : ModPlayer
{
    public bool largeWhipIncrease = false;

    public override void PostUpdateEquips()
    {
        largeWhipIncrease = false;

        for (int i = 3; i < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); i++)
        {
            if ((Player.armor[0].type == ItemID.TikiMask && Player.armor[1].type == ItemID.TikiShirt && Player.armor[2].type == ItemID.TikiPants) || (Player.armor[0].type == ItemID.ObsidianHelm && Player.armor[1].type == ItemID.ObsidianShirt && Player.armor[2].type == ItemID.ObsidianPants) || (Player.armor[0].type == ItemID.BeeHeadgear && Player.armor[1].type == ItemID.BeeBreastplate && Player.armor[2].type == ItemID.BeeGreaves) || (Player.armor[0].type == ItemID.SpiderMask && Player.armor[1].type == ItemID.SpiderBreastplate && Player.armor[2].type == ItemID.SpiderGreaves))
            {
                largeWhipIncrease = true;
                break;
            }
        }
        if (!largeWhipIncrease)
        {
            Player.whipRangeMultiplier -= 0.65f;
        }
    }
}
