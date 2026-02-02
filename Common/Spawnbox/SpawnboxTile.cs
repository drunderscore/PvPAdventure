using System.Linq;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

public class SpawnboxTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
    {
#if DEBUG
        //return true;
#endif

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
#if DEBUG
        //return true;
#endif

        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
    {
#if DEBUG
        //return true;
#endif

        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }
}