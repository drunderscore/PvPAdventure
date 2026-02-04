using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

internal class DisableTombstones : ModSystem
{
    public override void Load()
    {
        // Prevent tombstones.
        On_Player.DropTombstone += (_, _, _, _, _) => { };
    }
}
