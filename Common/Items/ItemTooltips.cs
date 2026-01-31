using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.Linq;
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

        // Possible EJ changes below
        if (item.type == ItemID.BeetleScaleMail)
        {
            TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
            if (setBonusLine != null)
            {
                setBonusLine.Text = "Set bonus:\nGain Beetles from player kills\nBeetles increase your melee damage and attack speed";
            }
        }

        if (item.type == ItemID.TikiMask || item.type == ItemID.TikiShirt || item.type == ItemID.TikiPants)
        {
            TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
            if (setBonusLine != null)
            {
                setBonusLine.Text = "Set bonus:\nIncreases your max number of minions\nIncreases whip range by 20%\nIncreases whip debuff duration against players by 150%\nPrevents whip range penalty";
            }
        }
        if (item.type == ItemID.ObsidianHelm || item.type == ItemID.ObsidianShirt || item.type == ItemID.ObsidianPants)
        {
            TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
            if (setBonusLine != null)
            {
                setBonusLine.Text = "Set bonus:\n\tIncreases whip range by 30% and speed by 15%\nIncreases summon damage by 15%\nPrevents whip range penalty";
            }
        }
        if (item.type == ItemID.BlandWhip || item.type == ItemID.ThornWhip ||
        item.type == ItemID.BoneWhip || item.type == ItemID.FireWhip ||
        item.type == ItemID.CoolWhip || item.type == ItemID.SwordWhip ||
        item.type == ItemID.ScytheWhip || item.type == ItemID.MaceWhip ||
        item.type == ItemID.RainbowWhip)
        {
            tooltips.Add(new TooltipLine(Mod, "WhipRangeWarning", "Range greatly reduced without certain Set Bonuses")
            {
                OverrideColor = new Color(255, 100, 100)
            });
            tooltips.Add(new TooltipLine(Mod, "SummonsArePlayers", "All whip debuffs apply to players, and effect all non-summon damage")
            {
                OverrideColor = new Color(100, 255, 100)
            });
        }

        if (item.type == ItemID.SpectrePickaxe || item.type == ItemID.ShroomiteDiggingClaw)
        {
            tooltips.Add(new TooltipLine(Mod, "MiningPowerChange", "Capable of mining Lihzahrd Bricks"));
        }
        if (item.type == ItemID.PhilosophersStone || item.type == ItemID.CharmofMyths)
        {
            tooltips.Add(new TooltipLine(Mod, "FullHPRespawn", "Gain full health upon returning to the land of the living"));
        }
        if (item.type == ItemID.ArcheryPotion)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("10% increased bow damage") ||
                    tooltips[i].Text.Contains("bow damage and 20%"))
                {
                    tooltips[i].Text = "20% increased arrow speed";
                    break;
                }
            }
            tooltips.Add(new TooltipLine(Mod, "ArcheryNerf", "No longer grants bow damage")
            {
                OverrideColor = new Color(255, 100, 100)
            });
        }
        if (item.type == ItemID.TempleKey)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("Opens the jungle temple door") ||
                    tooltips[i].Text.Contains("temple door"))
                {
                    tooltips[i].Text = "Opens the jungle temple";
                    break;
                }
            }
            tooltips.Add(new TooltipLine(Mod, "LihzahrdKey", "Opens the jungle temple's chests")
            {
                OverrideColor = new Color(255, 100, 100)
            });
        }
        if (item.type == ItemID.ShadowKey)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("Opens all Shadow Chests and Obsidian Lock Boxes") ||
                    tooltips[i].Text.Contains("all"))
                {
                    tooltips[i].Text = "Opens Shadow Chests and Obsidian Lock Boxes";
                    break;
                }
            }
            tooltips.Add(new TooltipLine(Mod, "ShadowKey", "Only opens a single Shadow Chest")
            {
                OverrideColor = new Color(255, 100, 100)
            });
        }
        if (item.type == ItemID.LunarCraftingStation)
        {
            tooltips.Add(new TooltipLine(Mod, "AllCraftTiles", "Counts as all crafting stations"));
        }
    }
}