using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class HittheChytty : ModPlayer
{
    public override void OnRespawn()
    {
        bool hasCharmOfMyths = false;

        for (int i = 3; i < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); i++)
        {
            if (Player.armor[i].type == ItemID.CharmofMyths || Player.armor[i].type == ItemID.PhilosophersStone)
            {
                hasCharmOfMyths = true;
                break;
            }
        }

        if (hasCharmOfMyths && !Player.HasBuff<UncoutHandboring>())
        {
            Player.statLife = Player.statLifeMax2;
        }
    }
}
