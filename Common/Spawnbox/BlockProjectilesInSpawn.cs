using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

internal class BlockProjectilesInSpawn : GlobalProjectile
{
    public override bool? CanCutTiles(Projectile projectile)
    {
        if (projectile.owner == Main.myPlayer)
        {
            var region = ModContent.GetInstance<RegionManager>()
                .GetRegionIntersecting(projectile.Hitbox.ToTileRectangle());

            if (region != null && !region.CanModifyTiles)
                return false;
        }

        return null;
    }
}
