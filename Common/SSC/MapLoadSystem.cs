using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC;

// Manually loads sections of the map.
// This is a neccessary hotfix.
[Autoload(Side = ModSide.Client)]
public sealed class MapLoadSystem : ModSystem
{
    private static bool loadPending;
    private static int loadDelay;

    private static bool rebuildPending;
    private static int secX;
    private static int secY;

    private static On_Main.orig_DrawToMap_Section origDrawToMapSection;

    private const int SectionsPerFrame = 4;

    public override void Load()
    {
        On_Main.DrawToMap += Hook_DrawToMap;
        On_Main.DrawToMap_Section += Hook_DrawToMap_Section;
    }

    public static void Request(int delayTicks)
    {
        loadPending = true;
        loadDelay = delayTicks;
    }

    private static void RequestRebuild()
    {
        rebuildPending = true;
        secX = 0;
        secY = 0;

        Main.mapReady = false;
        Main.clearMap = true;
        Main.loadMap = true;
        Main.loadMapLock = true;
        Main.loadMapLastX = 0;
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!SSC.IsEnabled)
            return;

        if (!loadPending)
            return;

        if (loadDelay-- > 0)
            return;

        loadPending = false;

        Main.Map.Load();
        RequestRebuild();
    }

    private void Hook_DrawToMap_Section(On_Main.orig_DrawToMap_Section orig, Main self, int x, int y)
    {
        if (origDrawToMapSection == null)
            origDrawToMapSection = orig;

        orig(self, x, y);
    }

    private void Hook_DrawToMap(On_Main.orig_DrawToMap orig, Main self)
    {
        orig(self);

        if (!rebuildPending)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!SSC.IsEnabled)
            return;

        if (!Main.mapEnabled)
            return;

        if (origDrawToMapSection == null)
            return;

        int maxSecX = (Main.maxTilesX + 199) / 200;
        int maxSecY = (Main.maxTilesY + 149) / 150;

        int budget = SectionsPerFrame;

        while (budget-- > 0)
        {
            if (secY >= maxSecY)
            {
                rebuildPending = false;

                Main.mapReady = true;
                Main.loadMap = false;
                Main.loadMapLock = false;

                return;
            }

            origDrawToMapSection(self, secX, secY);

            secX++;

            if (secX >= maxSecX)
            {
                secX = 0;
                secY++;
            }
        }
    }
}
