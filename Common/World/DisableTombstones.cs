using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

internal class DisableTombstones : ModSystem
{
    public override void Load()
    {
        // Prevent tombstones.
        On_Player.DropTombstone += SuppressTombstoneDrop;
    }

    public override void Unload()
    {
        On_Player.DropTombstone -= SuppressTombstoneDrop;
    }

    private static void SuppressTombstoneDrop(
        On_Player.orig_DropTombstone orig,
        Player self,
        long coinsOwned,
        NetworkText deathText,
        int hitDirection)
    {
        if (!ModContent.GetInstance<ServerConfig>().DisableTombstones)
        {
            orig(self, coinsOwned, deathText, hitDirection);
        }
    }
}
