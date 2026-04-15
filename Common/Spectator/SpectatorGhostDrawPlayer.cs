using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using PvPAdventure.Core.Config;

namespace PvPAdventure.Common.Spectator;

internal sealed class SpectatorGhostDrawPlayer : ModPlayer
{
    private static bool ShouldHideGhost(Player drawPlayer)
    {
        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
            return false;

        if (drawPlayer.whoAmI == Main.myPlayer)
            return false;

        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();
        return !config.DrawGhostsForOthers;
    }

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        Player drawPlayer = drawInfo.drawPlayer;
        if (!ShouldHideGhost(drawPlayer))
            return;

        foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
            layer.Hide();
    }
}