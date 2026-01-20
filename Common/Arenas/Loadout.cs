using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Arenas;

// Class used to define a player's loadout for arenas, a new gamemode..
public class Loadout
{
    public string Name { get; set; }

    public ItemDefinition Head { get; set; }
    public ItemDefinition Body { get; set; }
    public ItemDefinition Legs { get; set; }

    public List<ItemDefinition> Accessories { get; set; } = [];
    public List<LoadoutItem> Inventory { get; set; } = [];

    public ItemDefinition GrapplingHook { get; set; }
}
public class LoadoutItem
{
    public ItemDefinition Item { get; set; } = new(ItemID.None);

    [DefaultValue(1)]
    public int Stack { get; set; } = 1;

    public LoadoutItem()
    {
    }

    public LoadoutItem(ItemDefinition item, int stack = 1)
    {
        Item = item;
        Stack = stack;
    }
}
