using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PvPAdventure.Content.Buffs;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Grants GodsProtection to players who take damage with Cross Necklace / Star Veil equipped
/// </summary>
public class CrossNecklaceChanges : ModPlayer
{
    public float CrossEnergy;

    public bool IsWearingCrossItem => HasCrossNecklaceOrStarVeil();

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

        if (CrossEnergy >= Player.statLifeMax2)
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

        if (HasFullHallowedArmor())
            return true;

        return false;
    }

    private bool HasFullHallowedArmor()
    {
        int head = Player.armor[0].type;
        int chest = Player.armor[1].type;
        int legs = Player.armor[2].type;

        bool isHallowedHead = head == ItemID.HallowedHelmet
            || head == ItemID.HallowedHeadgear
            || head == ItemID.HallowedMask;

        return isHallowedHead
            && chest == ItemID.HallowedPlateMail
            && legs == ItemID.HallowedGreaves;
    }
}