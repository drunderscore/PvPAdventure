using PvPAdventure.Core.Config.ConfigElements.CustomItemDefinitions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Arenas;

// Class used to define a player's loadout for arenas, a new gamemode developed during 2026-01!
public class Loadout
{
    public string Name { get; set; } = "";
    public Armor Armor { get; set; } = new();
    public Accessories Accessories { get; set; } = new();
    public Equipment Equipment { get; set; } = new();
    public List<LoadoutItem> Inventory { get; set; } = [];
}
public class Armor
{
    [CustomModConfigItem(typeof(ItemHeadDefinitionElement))]
    public ItemDefinition Head { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemBodyDefinitionElement))]
    public ItemDefinition Body { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemLegsDefinitionElement))]
    public ItemDefinition Legs { get; set; } = new(ItemID.None);
}

public class Accessories
{
    [CustomModConfigItem(typeof(ItemAccessoryDefinitionElement))]
    public ItemDefinition Accessory1 { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemAccessoryDefinitionElement))]
    public ItemDefinition Accessory2 { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemAccessoryDefinitionElement))]
    public ItemDefinition Accessory3 { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemAccessoryDefinitionElement))]
    public ItemDefinition Accessory4 { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemAccessoryDefinitionElement))]
    public ItemDefinition Accessory5 { get; set; } = new(ItemID.None);
}

public class Equipment
{
    [CustomModConfigItem(typeof(ItemGrapplingHookDefinitionElement))]
    public ItemDefinition GrapplingHook { get; set; } = new(ItemID.None);

    [CustomModConfigItem(typeof(ItemMountDefinitionElement))]
    public ItemDefinition Mount { get; set; } = new(ItemID.None);
}

/// <summary>
/// Consists of a item and a stack.
/// A lot of logic to clamp the stack based on the item type.
/// </summary>
public class LoadoutItem
{
    private ItemDefinition _item = new(ItemID.None);
    private int _stack = 1;

    public ItemDefinition Item
    {
        get => _item;
        set
        {
            _item = value ?? new ItemDefinition(ItemID.None);
            Stack = _stack; // re-clamp
        }
    }

    [DefaultValue(1)]
    [Range(1, 9999)]
    public int Stack
    {
        get => _stack;
        set
        {
            int type = Item?.Type ?? 0;
            int max = GetMaxStack(type);
            int clamped = Math.Clamp(value, 1, max);
            // useless -_-
            //if (value < 1 || value > max)
            //    Main.NewText("Warning: Item stack out of bounds!", Color.OrangeRed);
            _stack = clamped;
        }
    }

    public static int GetMaxStack(int type)
    {
        if (type <= 0)
            return 1;

        if (ContentSamples.ItemsByType.TryGetValue(type, out Item sample))
        {
            return Math.Max(1, sample.maxStack);
        }

        // Fallback
        Item temp = new();
        temp.SetDefaults(type);
        return Math.Max(1, temp.maxStack);
    }
}


