using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using PvPAdventure.Core.Config;

namespace PvPAdventure.Common.Spectator;

internal sealed class SpectatorGhostDrawPlayer : ModPlayer
{
    internal static bool ShouldDrawGhost(Player drawPlayer)
    {
        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
            return true;

        if (drawPlayer.whoAmI == Main.myPlayer)
            return true;

        ClientConfig config = ModContent.GetInstance<ClientConfig>();
        return config.DrawGhostsForOthers;
    }

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        Player drawPlayer = drawInfo.drawPlayer;
        if (ShouldDrawGhost(drawPlayer))
            return;

        foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
            layer.Hide();
    }
}
