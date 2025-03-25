using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;

namespace PvPAdventure;

public class AdventureItem : GlobalItem
{
    public static readonly bool[] RecallItems =
        ItemID.Sets.Factory.CreateBoolSet(ItemID.MagicMirror, ItemID.CellPhone, ItemID.IceMirror, ItemID.Shellphone,
            ItemID.ShellphoneSpawn);

    public override void SetDefaults(Item item)
    {
        if (RecallItems[item.type])
        {
            var recallTime = ModContent.GetInstance<AdventureConfig>().RecallFrames;
            item.useTime = recallTime * 2;
            item.useAnimation = recallTime * 2;
        }
    }

    public override bool CanUseItem(Item item, Player player)
    {
        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    // NOTE: This will not reemove already-equipped accessories from players.
    public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
    {
        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    public override bool? CanBeChosenAsAmmo(Item ammo, Item weapon, Player player)
    {
        if (ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => ammo.type == itemDefinition.Type))
            return false;

        return null;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var itemDefinition = new ItemDefinition(item.type);
        if (adventureConfig.Combat.PlayerDamageBalance.ItemDamageMultipliers.TryGetValue(itemDefinition,
                out var multiplier))
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
    }

    public override bool? PrefixChance(Item item, int pre, UnifiedRandom rand)
    {
        // Prevent the item silly words hehehehe from spawning with a prefix, being placed into a reforge window, and loading with a prefix.
        if ((pre == -1 || pre == -3 || pre > 0) && ModContent.GetInstance<AdventureConfig>().RemovePrefixes)
            return false;

        return null;
    }

    // This is likely unnecessary if we are overriding PrefixChance, but might as well.
    public override bool CanReforge(Item item) => !ModContent.GetInstance<AdventureConfig>().RemovePrefixes;
}