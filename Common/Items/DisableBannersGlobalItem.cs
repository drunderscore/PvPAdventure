using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

internal class DisableBannersGlobalItem : GlobalItem
{
    public override void OnSpawn(Item item, IEntitySource source)
    {
        if (item.createTile == TileID.Banners)
        {
            item.active = false;
            item.TurnToAir();
        }
    }

    public override void UpdateInventory(Item item, Player player)
    {
        if (item.createTile == TileID.Banners)
        {
            item.TurnToAir();
        }
    }
}
