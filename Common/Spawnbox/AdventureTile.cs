using System.Linq;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

public class AdventureTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanExplode(int i, int j, int type)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanPlace(int i, int j, int type)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }
}