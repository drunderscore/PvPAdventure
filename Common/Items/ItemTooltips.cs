using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Items;

// - Adds PvP damage multiplier tooltip based on config.
// - Adds "Disabled" tooltip when item is prevented.
// - Adds summon restriction tooltips for Empress Butterfly and Queen Slime Crystal.
public class ItemTooltips : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        var adventureConfig = ModContent.GetInstance<ServerConfig>();

        var itemDefinition = new ItemDefinition(item.type);
        if (adventureConfig.WeaponBalance.Damage.ItemDamage.TryGetValue(itemDefinition, out var multiplier))
        {
            // FIXME: The mod config is very imprecise with floating points. Do some rounding to make the UI cleaner.
            tooltips.Add(new TooltipLine(Mod, "CombatPlayerDamageBalance",
                $"-{(int)((1.0f - multiplier) * 100)}% PvP damage")
            {
                IsModifier = true,
                IsModifierBad = true
            });
        }

        if (adventureConfig.PreventUse.Contains(itemDefinition))
        {
            tooltips.Add(new TooltipLine(Mod, "Disabled", Language.GetTextValue("Mods.PvPAdventure.Item.Disabled"))
            {
                OverrideColor = Color.Red
            });
        }
        else
        {
            var isUnderground = Main.LocalPlayer.position.Y > Main.worldSurface * 16;
            var isHallow = Main.LocalPlayer.ZoneHallow;
            if (item.type == ItemID.EmpressButterfly)
            {
                if (isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundEmpressButterfly",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundEmpressButterfly"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
            else if (item.type == ItemID.QueenSlimeCrystal)
            {
                if (isHallow && isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundQueenSlimeCrystal",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundQueenSlimeCrystal"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
        }
    }
}
