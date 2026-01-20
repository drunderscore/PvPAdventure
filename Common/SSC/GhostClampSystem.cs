using Microsoft.Xna.Framework;
using PvPAdventure.Core.Debug;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

[Autoload(Side = ModSide.Server)]
public sealed class SSCGhostClampSystem : ModSystem
{
    public override void PostUpdatePlayers()
    {
        if (!SSC.IsEnabled || Main.netMode != NetmodeID.Server)
            return;

        Vector2 spawnPos = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f - 48f);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            if (!p.ghost)
                continue;

            p.position = spawnPos;
            p.velocity = Vector2.Zero;

            // Push authoritative state to clients
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.whoAmI);
        }
    }
}
