using System;
using Terraria;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

public class ResourceBarsDrawer
{
    public static void DrawResourceBarsLikeVanilla(Player player)
    {
        if (player?.active != true)
            return;

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        int oldLife = local.statLife;
        int oldLifeMax = local.statLifeMax;
        int oldLifeMax2 = local.statLifeMax2;
        int oldMana = local.statMana;
        int oldManaMax = local.statManaMax;
        int oldManaMax2 = local.statManaMax2;
        int oldConsumedLifeCrystals = local.ConsumedLifeCrystals;
        int oldConsumedLifeFruit = local.ConsumedLifeFruit;
        int oldConsumedManaCrystals = local.ConsumedManaCrystals;

        try
        {
            CopyResourceStats(player, local);

            Main.ResourceSetsManager.Draw();
            Main.ResourceSetsManager.TryToHoverOverResources();
        }
        finally
        {
            local.statLife = oldLife;
            local.statLifeMax = oldLifeMax;
            local.statLifeMax2 = oldLifeMax2;
            local.statMana = oldMana;
            local.statManaMax = oldManaMax;
            local.statManaMax2 = oldManaMax2;
            local.ConsumedLifeCrystals = oldConsumedLifeCrystals;
            local.ConsumedLifeFruit = oldConsumedLifeFruit;
            local.ConsumedManaCrystals = oldConsumedManaCrystals;
        }
    }

    private static void CopyResourceStats(Player from, Player to)
    {
        to.statLife = from.statLife;
        to.statLifeMax = from.statLifeMax;
        to.statLifeMax2 = from.statLifeMax2;
        to.statMana = from.statMana;
        to.statManaMax = from.statManaMax;
        to.statManaMax2 = from.statManaMax2;
        to.ConsumedLifeCrystals = from.ConsumedLifeCrystals;
        to.ConsumedLifeFruit = from.ConsumedLifeFruit;
        to.ConsumedManaCrystals = from.ConsumedManaCrystals;
    }

    [Obsolete("Bloat")]
    public static void DrawResourceBarsLikeVanilla_Old(Player player)
    {
        if (player?.active != true)
            return;

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        int oldLife = local.statLife;
        int oldLifeMax = local.statLifeMax;
        int oldLifeMax2 = local.statLifeMax2;
        int oldMana = local.statMana;
        int oldManaMax = local.statManaMax;
        int oldManaMax2 = local.statManaMax2;
        int oldConsumedLifeCrystals = local.ConsumedLifeCrystals;
        int oldConsumedLifeFruit = local.ConsumedLifeFruit;
        int oldConsumedManaCrystals = local.ConsumedManaCrystals;

        Item oldHoverItem = Main.HoverItem?.Clone() ?? new Item();
        string oldHoverItemName = Main.hoverItemName;
        bool oldMouseText = Main.mouseText;
        int oldRare = Main.rare;

        string resourceHoverText = "";
        bool resourceHover = false;
        bool resourceHoverIsMana = false;

        try
        {
            Main.HoverItem = new Item();
            Main.hoverItemName = "";
            Main.mouseText = false;
            Main.rare = 0;

            local.statLife = player.statLife;
            local.statLifeMax = player.statLifeMax;
            local.statLifeMax2 = player.statLifeMax2;
            local.statMana = player.statMana;
            local.statManaMax = player.statManaMax;
            local.statManaMax2 = player.statManaMax2;
            local.ConsumedLifeCrystals = player.ConsumedLifeCrystals;
            local.ConsumedLifeFruit = player.ConsumedLifeFruit;
            local.ConsumedManaCrystals = player.ConsumedManaCrystals;

            Main.ResourceSetsManager.Draw();

            resourceHoverText = Main.hoverItemName ?? "";
            resourceHover = Main.mouseText && !string.IsNullOrWhiteSpace(resourceHoverText);
            resourceHoverIsMana = IsManaTooltip(resourceHoverText, player);
        }
        finally
        {
            local.statLife = oldLife;
            local.statLifeMax = oldLifeMax;
            local.statLifeMax2 = oldLifeMax2;
            local.statMana = oldMana;
            local.statManaMax = oldManaMax;
            local.statManaMax2 = oldManaMax2;
            local.ConsumedLifeCrystals = oldConsumedLifeCrystals;
            local.ConsumedLifeFruit = oldConsumedLifeFruit;
            local.ConsumedManaCrystals = oldConsumedManaCrystals;

            RestoreHover(oldHoverItem, oldHoverItemName, oldMouseText, oldRare);
        }

        //if (resourceHover)
        //    ApplyResourceTooltip(player, resourceHoverIsMana);
    }

    private static void RestoreHover(Item hoverItem, string hoverItemName, bool mouseText, int rare)
    {
        Main.HoverItem = hoverItem ?? new Item();
        Main.hoverItemName = hoverItemName ?? "";
        Main.mouseText = mouseText;
        Main.rare = rare;
    }

    private static void ApplyResourceTooltip(Player player, bool mana)
    {
        string text = mana
            ? $"Mana: {Math.Max(0, player.statMana)}/{player.statManaMax2}"
            : $"Life: {Math.Max(0, player.statLife)}/{player.statLifeMax2}";

        Main.HoverItem = new Item();
        Main.hoverItemName = text;
        Main.mouseText = true;
        Main.rare = 0;

        Main.instance.MouseText(text, 0, 0);
    }

    private static bool IsManaTooltip(string text, Player player)
    {
        if (text.Contains("mana", StringComparison.OrdinalIgnoreCase))
            return true;

        return ContainsStatPair(text, player.statMana, player.statManaMax2);
    }

    private static bool ContainsStatPair(string text, int current, int maximum)
    {
        return text.Contains(current.ToString()) && text.Contains(maximum.ToString());
    }
}