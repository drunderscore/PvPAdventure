using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;

[Autoload(Side = ModSide.Client)]
internal sealed class DisableGhostDrawSystem : ModSystem
{
    public override void Load()
    {
        On_LegacyPlayerRenderer.DrawGhost += OnDrawGhost;
    }

    public override void Unload()
    {
        On_LegacyPlayerRenderer.DrawGhost -= OnDrawGhost;
    }

    private void OnDrawGhost(On_LegacyPlayerRenderer.orig_DrawGhost orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position, float shadow)
    {
        if (!ShouldDrawGhost(drawPlayer))
            return;

        orig(self, camera, drawPlayer, position, shadow);
    }

    public static bool ShouldDrawGhost(Player drawPlayer)
    {
        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
            return true;

        if (drawPlayer.whoAmI == Main.myPlayer)
            return true;

        Player local = Main.LocalPlayer;

        if (local?.active == true && local.ghost)
            return true;

        return ModContent.GetInstance<ClientConfig>().DrawSpectators;
    }
}

//internal sealed class SpectatorGhostDrawPlayer : ModPlayer
//{
//    internal static bool ShouldDrawGhost(Player drawPlayer)
//    {
//        if (drawPlayer == null || !drawPlayer.active || !drawPlayer.ghost)
//            return true;

//        if (drawPlayer.whoAmI == Main.myPlayer)
//            return true;

//        ClientConfig config = ModContent.GetInstance<ClientConfig>();
//        return config.DrawGhostsForOthers;
//    }

//    public override void HideDrawLayers(PlayerDrawSet drawInfo)
//    {
//        Player drawPlayer = drawInfo.drawPlayer;
//        if (ShouldDrawGhost(drawPlayer))
//            return;

//        foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
//            layer.Hide();
//    }
//}