using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

/// <summary>
/// Keeps track of item IDs and number of blocks/walls placed during a match.
/// </summary>
internal sealed class ItemPlacementTileTracker : GlobalTile
{
    public override void PlaceInWorld(int i, int j, int type, Item item)
    {
        RecordPlacedItem(item);
    }

    internal static void RecordPlacedItem(Item item)
    {
        if (item == null || item.IsAir)
            return;

        MatchStatsPlayer.RecordLocalItemStat(MatchStatKey.TilesPlaced, item.type);
    }
}

internal sealed class ItemPlacementWallTracker : GlobalWall
{
    public override void PlaceInWorld(int i, int j, int type, Item item)
    {
        ItemPlacementTileTracker.RecordPlacedItem(item);
    }
}
