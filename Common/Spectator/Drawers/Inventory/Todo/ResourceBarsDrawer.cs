using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
