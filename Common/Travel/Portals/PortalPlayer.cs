using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

internal sealed class PortalPlayer : ModPlayer
{
    public override bool CanHitNPC(NPC target)
    {
        if (target?.ModNPC is PortalNPC portal && PortalSystem.IsFriendlyPortal(Player, portal))
            return false;

        return true;
    }
}
