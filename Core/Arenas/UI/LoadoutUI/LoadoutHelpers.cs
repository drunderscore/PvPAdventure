using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Core.Arenas.UI.LoadoutUI;

public class LoadoutDef
{
    public string Name;

    public int Head;
    public int Body;
    public int Legs;

    public List<int> Accessories = [];              // 0-6, max 6 items
    public List<LoadoutItem> Hotbar = [];            // 0–10, max 10 items
    public int EquipmentHook;
}

public static class LoadoutApplier
{
    public static Player BuildPlayer(Player source, LoadoutDef def)
    {
        Player p = (Player)source.clientClone();

        // armor
        p.armor[0].SetDefaults(def.Head);
        p.armor[1].SetDefaults(def.Body);
        p.armor[2].SetDefaults(def.Legs);

        // accessories
        for (int i = 0; i < def.Accessories.Count && i < 5; i++)
            p.armor[3 + i].SetDefaults(def.Accessories[i]);

        // inventory
        for (int i = 0; i < def.Hotbar.Count && i < 10; i++)
        {
            var li = def.Hotbar[i];
            p.inventory[i].SetDefaults(li.Type);
            p.inventory[i].stack = li.Stack;
        }

        // eqipment
        p.miscEquips[4] = new Item(ItemID.SpookyHook);

        return p;
    }
}
public readonly struct LoadoutItem
{
    public readonly int Type;
    public readonly int Stack;

    public LoadoutItem(int type, int stack = 1)
    {
        Type = type;
        Stack = stack;
    }
}
