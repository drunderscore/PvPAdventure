using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;

internal class FloodlightSpectatorSystem : ModSystem
{
    int strength = 1;

    public override void Load()
    {
        On_TileLightScanner.GetTileLight += HackLight;
    }

    private void HackLight(On_TileLightScanner.orig_GetTileLight orig, TileLightScanner self, int x, int y, out Vector3 outputColor)
    {
        orig(self, x, y, out outputColor);

        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return;

        //if (strength > 0)
        //    outputColor += Vector3.One * strength;
    }
}
