using System;
using System.Collections.Generic;
using System.Text;
using Terraria.ModLoader;

namespace PvPAdventure
{
    class DebuffChanges : ModPlayer
    {
        public override void UpdateBadLifeRegen()
        {
            // Get the mod instance to access the config
            var config = ModContent.GetInstance<AdventureConfig>();

            if (Player.HasBuff(39)) // On Fire!
            {
                Player.lifeRegen += config.Combat.Debuff.CursedInfernoDps;
                Player.lifeRegenTime = 0f;
            }
            if (Player.HasBuff(70)) // Poisoned
            {
                Player.lifeRegen += config.Combat.Debuff.VenomDps;
                Player.lifeRegenTime = 0f;
            }
        }
    }
}
