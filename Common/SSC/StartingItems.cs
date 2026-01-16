using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC;

public static class StartingItems
{
    public static void ApplyStartItems(Player player)
    {
        var config = ModContent.GetInstance<ServerConfig>();

        int slot = 0;

        foreach (var (itemDef, stack) in config.StartItems)
        {
            if (itemDef.IsUnloaded)
                continue;

            if (slot >= player.inventory.Length)
                break;

            Item item = new();
            item.SetDefaults(itemDef.Type);
            item.stack = stack;

            player.inventory[slot++] = item;

            Log.Chat("Start item " + itemDef.DisplayName + " added");
        }
    }
    public static void ApplyStartLife(Player player)
    {
        var config = ModContent.GetInstance<ServerConfig>();

        int targetLife = Utils.Clamp(config.StartLife, 100, 500);

        int lifeAboveBase = targetLife - 100;

        int crystals = Math.Min(lifeAboveBase / 20, Player.LifeCrystalMax);
        lifeAboveBase -= crystals * 20;

        int fruits = Math.Min(lifeAboveBase / 5, Player.LifeFruitMax);

        player.ConsumedLifeCrystals = crystals;
        player.ConsumedLifeFruit = fruits;

        player.statLife = player.statLifeMax;
    }

    public static void ApplyStartMana(Player player)
    {
        var config = ModContent.GetInstance<ServerConfig>();

        int targetMana = Utils.Clamp(config.StartMana, 20, 200);

        int stars = (targetMana - 20) / 20;
        stars = Math.Clamp(stars, 0, 9);

        player.ConsumedManaCrystals = stars;
        player.statMana = player.statManaMax;
    }
}
