using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC;

[Autoload(Side = ModSide.Client)]
internal sealed class MapScanSystem : ModSystem
{
    private const int CellsPerTick = 50_000;

    private static bool _requested;
    private static bool _scanning;

    private static int _minX;
    private static int _maxX;
    private static int _minY;
    private static int _maxY;

    private static int _x;
    private static int _y;

    private static long _total;
    private static long _revealed;

    public static void RequestScan()
    {
        _requested = true;
    }

    public override void PostUpdateEverything()
    {
        if (!_requested && !_scanning)
            return;

        if (Main.netMode == NetmodeID.Server)
            return;

        if (Main.Map == null)
            return;

        if (_requested)
        {
            StartScan();
            _requested = false;
        }

        StepScan();
    }

    private static void StartScan()
    {
        int edge = WorldMap.BlackEdgeWidth;

        _minX = edge;
        _minY = edge;
        _maxX = Main.Map.MaxWidth - edge;
        _maxY = Main.Map.MaxHeight - edge;

        if (_maxX <= _minX || _maxY <= _minY)
        {
            _minX = 0;
            _minY = 0;
            _maxX = Main.Map.MaxWidth;
            _maxY = Main.Map.MaxHeight;
        }

        _x = _minX;
        _y = _minY;

        _revealed = 0;
        _total = (long)(_maxX - _minX) * (_maxY - _minY);

        _scanning = _total > 0;
    }

    private static void StepScan()
    {
        if (!_scanning)
            return;

        for (int i = 0; i < CellsPerTick && _scanning; i++)
        {
            if (Main.Map.IsRevealed(_x, _y))
                _revealed++;

            _x++;

            if (_x >= _maxX)
            {
                _x = _minX;
                _y++;

                if (_y >= _maxY)
                    FinishScan();
            }
        }
    }

    private static void FinishScan()
    {
        _scanning = false;

        double pct = _total <= 0 ? 0.0 : (_revealed / (double)_total) * 100.0;
        Log.Chat($"Map Explored: {pct:0.00}%");
    }
}
