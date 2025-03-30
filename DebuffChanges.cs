using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace PvPAdventure
{
    internal class CursedFlameDebuffPlayer : ModPlayer
    {
        //Changes dps for varius player debuffs
        public override void UpdateBadLifeRegen()
        {
            if (base.Player.HasBuff(39))
            {
                base.Player.lifeRegen += 7;
                base.Player.lifeRegenTime = 0f;
            }
            if (base.Player.HasBuff(70))
            {
                base.Player.lifeRegen += 14;
                base.Player.lifeRegenTime = 0f;
            }
        }
    }
}
