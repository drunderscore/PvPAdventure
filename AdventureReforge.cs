﻿using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureReforge : GlobalItem
{
    public override void OnCreated(Item item, ItemCreationContext context)
    {
        if (item.prefix != 0)
            item.ResetPrefix();
    }

    public override void OnSpawn(Item item, IEntitySource source)
    {
        if (item.prefix != 0)
            item.ResetPrefix();
    }

    public override bool OnPickup(Item item, Player player)
    {
        if (item.prefix != 0)
            item.ResetPrefix();
        return true;
    }

    public override bool CanReforge(Item item)
    {
        return false;
    }
}
