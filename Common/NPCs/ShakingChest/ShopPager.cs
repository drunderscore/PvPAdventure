using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public class ShopPager : ModSystem
{
    public const int PageSize = 37;
    public static int CurrentPage { get; private set; } = 0;
    public static int TotalPages { get; private set; } = 1;
    private static Item[] _cachedItems = null;
    private static NPC _cachedNPC = null;

    public static void OpenShop(Item[] allItems, NPC npc)
    {
        _cachedItems = allItems;
        _cachedNPC = npc;
        var validItems = allItems.Where(i => i != null && !i.IsAir).ToArray();
        TotalPages = (int)Math.Ceiling(validItems.Length / (float)PageSize);
        CurrentPage = 0;
        LoadPage(validItems);
    }

    public static void GoToNextPage()
    {
        if (_cachedItems == null) return;
        var validItems = _cachedItems.Where(i => i != null && !i.IsAir).ToArray();
        CurrentPage = (CurrentPage + 1) % TotalPages;
        LoadPage(validItems);
    }

    public static void GoToPrevPage()
    {
        if (_cachedItems == null) return;
        var validItems = _cachedItems.Where(i => i != null && !i.IsAir).ToArray();
        CurrentPage = (CurrentPage - 1 + TotalPages) % TotalPages;
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

        if (TotalPages > 1)
        {
            var next = new Item(ModContent.ItemType<NextPageItem>());
            next.SetNameOverride($"Next Page ({CurrentPage + 1}/{TotalPages})");
            next.shopCustomPrice = 0;
            next.value = 0;
            shopItems[38] = next;

            var prev = new Item(ModContent.ItemType<PrevPageItem>());
            prev.SetNameOverride($"Previous Page ({CurrentPage + 1}/{TotalPages})");
            prev.shopCustomPrice = 0;
            prev.value = 0;
            shopItems[39] = prev;
        }
    }

    public static void Reset()
    {
        CurrentPage = 0;
        _cachedItems = null;
        _cachedNPC = null;
    }
}