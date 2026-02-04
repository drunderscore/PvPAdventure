using Terraria;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

// Debug system to ensure the MapLoadSystem for SSC works as intended.
// Prints the map explored % when the player joins a world.

#if DEBUG
[Autoload(Side = ModSide.Client)]
internal sealed class DebugMapScan : ModSystem
{
    public override void PostUpdateEverything()
    {
        // Only run once per x seconds.
        const int delayInSeconds = 55;
        if (Main.GameUpdateCount % (60*delayInSeconds) != 0)
            return;

        WorldMap map = Main.Map;

        if (map == null)
            return;

        // Compute scan bounds: prefer skipping the black border, but clamp to valid indices.
        int edge = WorldMap.BlackEdgeWidth;
        int w = map.MaxWidth;
        int h = map.MaxHeight;
        int minX = Utils.Clamp(edge, 0, w);
        int minY = Utils.Clamp(edge, 0, h);
        int maxX = Utils.Clamp(w - edge, 0, w);
        int maxY = Utils.Clamp(h - edge, 0, h);

        // If the edge crop collapses the range, fall back to scanning the full map.
        if (maxX <= minX)
            minX = 0;

        if (maxY <= minY)
            minY = 0;

        if (maxX <= minX)
            maxX = w;

        if (maxY <= minY)
            maxY = h;

        // Total cells in the scan rectangle.
        long total = (long)(maxX - minX) * (maxY - minY);

        if (total <= 0)
            return;

        // Walk the rectangle and count revealed tiles.
        long revealed = 0;
        int x = minX;
        int y = minY;

        while (y < maxY)
        {
            if (map.IsRevealed(x, y))
                revealed++;

            x++;

            if (x < maxX)
                continue;

            x = minX;
            y++;
        }

        // Report explored percentage.
        double pct = revealed * 100.0 / total;
        Log.Chat($"Map Explored: {pct:0.0000}%");
    }
}
#endif
