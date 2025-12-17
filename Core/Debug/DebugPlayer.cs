
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal class DebugPlayer : ModPlayer
{
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        //Player.respawnTimer = 120; // 2 seconds
        Player.respawnTimer = 600; // 10 seconds
        //Player.respawnTimer = 0;
        base.Kill(damage, hitDirection, pvp, damageSource);
    }
}
#endif
