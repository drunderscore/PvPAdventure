using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Arenas;

// Class used to define a player's loadout for arenas, a new gamemode..
public class Loadout
{
    public string Name { get; set; } = "";
    public Armor Armor { get; set; } = new();
    public Accessories Accessories { get; set; } = new();
    public List<LoadoutItem> Inventory { get; set; } = [];
    public ItemDefinition GrapplingHook { get; set; } = new(ItemID.None);
    public ItemDefinition Mount { get; set; } = new(ItemID.None);
}
public class Armor
{
    public ItemDefinition Head { get; set; } = new(ItemID.None);
    public ItemDefinition Body { get; set; } = new(ItemID.None);
    public ItemDefinition Legs { get; set; } = new(ItemID.None);
}

public class Accessories
{
    public ItemDefinition Accessory1 { get; set; } = new(ItemID.None);
    public ItemDefinition Accessory2 { get; set; } = new(ItemID.None);
    public ItemDefinition Accessory3 { get; set; } = new(ItemID.None);
    public ItemDefinition Accessory4 { get; set; } = new(ItemID.None);
    public ItemDefinition Accessory5 { get; set; } = new(ItemID.None);
}

/// <summary>
/// Consists of a item and a stack.
/// A lot of logic to clamp the stack based on the item type.
/// </summary>
public class LoadoutItem
{
    public ItemDefinition Item
    {
        get => _item;
        set
        {
            _item = value ?? new ItemDefinition(ItemID.None);
            Stack = _stack; // re-clamp when item changes
        }
    }

    [DefaultValue(1)]
    public int Stack
    {
        get => _stack;
        set
        {
            int max = 1;

            int type = Item?.Type ?? 0;
            if (type > 0)
            {
                Item temp = new Item();
                temp.SetDefaults(type);
                max = temp.maxStack;

                if (max < 1)
                    max = 1;
            }

            _stack = Math.Clamp(value, 1, max);
        }
    }

    private ItemDefinition _item = new(ItemID.None);
    private int _stack = 1;

    public LoadoutItem()
    {
    }

    public LoadoutItem(ItemDefinition item, int stack = 1)
    {
        Item = item;
        Stack = stack;
    }
}
