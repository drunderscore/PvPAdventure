using System.Linq;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

// - Blocks use of specific summon items based on biome/depth rules.
// - Blocks item use based on PreventUse config.
// - Blocks equipping accessories based on PreventUse config.
// - Blocks ammo selection based on PreventUse config.
public class BannedItems : GlobalItem
{
    public override bool CanUseItem(Item item, Player player)
    {
        var isUnderground = player.position.Y > Main.worldSurface * 16;
        var isHallow = player.ZoneHallow;

        if (item.type == ItemID.EmpressButterfly)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.QueenSlimeCrystal)
        {
            if (isUnderground && isHallow)
                return false;
        }

        return !ModContent.GetInstance<ServerConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    // NOTE: This will not remove already-equipped accessories from players.
    public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
    {
        return !ModContent.GetInstance<ServerConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    public override bool? CanBeChosenAsAmmo(Item ammo, Item weapon, Player player)
    {
        if (ModContent.GetInstance<ServerConfig>().PreventUse
            .Any(itemDefinition => ammo.type == itemDefinition.Type))
            return false;

        return null;
    }

    public override bool? CanAutoReuseItem(Item item, Player player)
    {
        if (ModContent.GetInstance<ServerConfig>().PreventAutoReuse.Contains(new(item.type)))
            return false;

        return null;
    }
}
