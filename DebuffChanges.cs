
using Terraria.ModLoader;

namespace PvPAdventure
{
    internal class PlayerDebuffModifier : ModPlayer
    {
        //Changes dps for varius player debuffs
        public override void UpdateBadLifeRegen()
        {
            if (base.Player.HasBuff(39))
            {
                base.Player.lifeRegen += 12;
                base.Player.lifeRegenTime = 0f;
            }
            if (base.Player.HasBuff(70))
            {
                base.Player.lifeRegen += 18;
                base.Player.lifeRegenTime = 0f;
            }
        }
    }
}
