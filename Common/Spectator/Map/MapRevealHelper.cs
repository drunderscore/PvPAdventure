using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Map;

internal static class MapRevealHelper
{
    public static bool Enabled => true;

    public static void RevealLocalMap()
    {
        if (!Enabled || Main.dedServ)
            return;

        for (int x = 0; x < Main.maxTilesX; x++)
        {
            for (int y = 0; y < Main.maxTilesY; y++)
            {
                if (WorldGen.InWorld(x, y))
                    Main.Map.Update(x, y, 255);
            }
        }

        Main.refreshMap = true;
    }

    public static void ClearLocalMap()
    {
        if (!Enabled || Main.dedServ)
            return;

        Main.Map.Clear();
        Main.refreshMap = true;
        Main.mapFullscreen = false;
        //Main.mapStyle = 0;
    }
}