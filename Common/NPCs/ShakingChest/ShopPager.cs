using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public class ShopPager : ModSystem
{
    public const int PageSize = 38;
    public static int CurrentPage { get; private set; } = 0;
    public static int TotalPages { get; private set; } = 1;

    public static void OpenShop(Item[] allItems, NPC npc)
    {
        var validItems = allItems.Where(i => i != null && !i.IsAir).ToArray();
        TotalPages = (int)Math.Ceiling(validItems.Length / (float)PageSize);
        CurrentPage = Math.Clamp(CurrentPage, 0, TotalPages - 1);
        LoadPage(validItems);
    }

    public static void NextPage(Item[] allItems, NPC npc)
    {
        var validItems = allItems.Where(i => i != null && !i.IsAir).ToArray();
        TotalPages = (int)Math.Ceiling(validItems.Length / (float)PageSize);
        CurrentPage = (CurrentPage + 1) % TotalPages;
        LoadPage(validItems);
    }

    private static void LoadPage(Item[] validItems)
    {
        var shopItems = Main.instance.shop[Main.npcShop].item;

        for (int i = 0; i < shopItems.Length; i++)
            shopItems[i] = new Item();

        int start = CurrentPage * PageSize;
        int end = Math.Min(start + PageSize, validItems.Length);

        for (int i = start; i < end; i++)
            shopItems[i - start] = validItems[i];
    }

    public static void Reset() => CurrentPage = 0;
}