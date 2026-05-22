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
/// Prevents melee speed bonuses from stacking when multiple glove accessories
/// </summary>
internal class GlovesDontStack : ModPlayer
{
    private static readonly HashSet<int> GloveIDs =
    [
        ItemID.FeralClaws,
        ItemID.PowerGlove,
        ItemID.BerserkerGlove,
        ItemID.MechanicalGlove,
        ItemID.FireGauntlet,
    ];

    private const float MeleeSpeedBonusPerGlove = 0.12f;

    public override void PostUpdateEquips()
    {
        int gloveCount = 0;

        for (int i = 3; i < 9; i++)
        {
            Item item = Player.armor[i];
            if (item != null && !item.IsAir && GloveIDs.Contains(item.type))
                gloveCount++;
        }

        if (gloveCount > 1)
            Player.meleeSpeed -= MeleeSpeedBonusPerGlove * (gloveCount - 1);
    }
}