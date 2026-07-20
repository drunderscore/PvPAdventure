using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PvPAdventure.Content.Buffs;

namespace PvPAdventure.Common.Combat.EJ
/// <summary>
/// Grants GodsProtection to players who take damage with cross Necklace / Star Viel equipped
/// </summary>
{
    public class CrossNecklaceChanges : ModPlayer
    {
        public float CrossEnergy;

        public override void PostHurt(Player.HurtInfo info)
        {
            if (info.PvP && HasCrossNecklaceOrStarVeil())
            {
                CrossEnergy += info.Damage * 2;
            }
        }

        public override void PostUpdate()
        {
            if (CrossEnergy > 0)
            {
                CrossEnergy -= 1;
                if (CrossEnergy < 0)
                    CrossEnergy = 0;
            }

            if (CrossEnergy > 400)
            {
                Player.AddBuff(ModContent.BuffType<GodsProtection>(), 2);
            }
        }

        private bool HasCrossNecklaceOrStarVeil()
        {
            for (int i = 3; i <= 9; i++)
            {
                Item item = Player.armor[i];
                if (item.type == ItemID.CrossNecklace || item.type == ItemID.StarVeil)
                    return true;
            }

            return false;
        }
    }
}