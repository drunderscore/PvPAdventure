using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

internal abstract class FilteredItemDefinitionElement : DefinitionElement<ItemDefinition>
{
    protected abstract bool IsValidItem(int type);

    protected override DefinitionOptionElement<ItemDefinition> CreateDefinitionOptionElement()
        => new ItemDefinitionOptionElement(Value, OptionScale);

    protected override List<DefinitionOptionElement<ItemDefinition>> CreateDefinitionOptionElementList()
    {
        var options = new List<DefinitionOptionElement<ItemDefinition>>
        {
            // Always include None (0)
            MakeOption(ItemID.None)
        };

        // Include only valid items
        for (int type = 1; type < ItemLoader.ItemCount; type++)
        {
            if (!IsValidItem(type))
                continue;

            if (ItemID.Sets.Deprecated[type])
                continue;

            options.Add(MakeOption(type));
        }

        return options;

        ItemDefinitionOptionElement MakeOption(int type)
        {
            var def = new ItemDefinition(type);
            var opt = new ItemDefinitionOptionElement(def, OptionScale);

            opt.OnLeftClick += (_, _) =>
            {
                Value = opt.Definition;
                UpdateNeeded = true;
                SelectionExpanded = false;
            };

            return opt;
        }
    }

    protected override List<DefinitionOptionElement<ItemDefinition>> GetPassedOptionElements()
    {
        var passed = new List<DefinitionOptionElement<ItemDefinition>>();

        foreach (var option in Options)
        {
            int type = option.Type;

            if (!IsValidItem(type))
                continue;

            if (type > 0 && ItemID.Sets.Deprecated[type])
                continue;

            // Name filter
            if (!Lang.GetItemNameValue(type).Contains(ChooserFilter.CurrentString, StringComparison.OrdinalIgnoreCase))
                continue;

            // Mod filter
            string modname = "Terraria";
            if (type >= ItemID.Count)
                modname = ItemLoader.GetItem(type).Mod.DisplayNameClean;

            if (modname.IndexOf(ChooserFilterMod.CurrentString, StringComparison.OrdinalIgnoreCase) == -1)
                continue;

            passed.Add(option);
        }

        return passed;
    }
}
