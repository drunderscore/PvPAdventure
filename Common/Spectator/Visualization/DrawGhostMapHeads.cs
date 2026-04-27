using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.Visualization;

/// <summary>
/// Draws all ghosts on the map with a custom icon instead of the default player head.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class GhostMapHeadSystem : ModSystem
{
    public override void Load()
    {
        On_MapHeadRenderer.DrawPlayerHead += HideGhostPlayersVanillaHeads;
    }

    public override void Unload()
    {
        On_MapHeadRenderer.DrawPlayerHead -= HideGhostPlayersVanillaHeads;
    }

    private static void HideGhostPlayersVanillaHeads(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor)
    {
        if (drawPlayer?.active == true && drawPlayer.ghost)
            return;

        orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
    }
}

internal sealed class GhostMapHeadLayer : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return;

        Texture2D ghostRight = Ass.Ghost.Value;
        Texture2D ghostLeft = Ass.GhostLeft.Value;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active != true || !player.ghost)
                continue;

            Texture2D texture = player.direction == -1 ? ghostLeft : ghostRight;

            MapOverlayDrawContext.DrawResult result = context.Draw(
                texture,
                player.Center / 16f,
                Color.White,
                new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.6f,
                scaleIfSelected: 2.2f,
                Alignment.Center);

            if (result.IsMouseOver)
                text = $"{player.name} (spectator)";
        }
    }
}