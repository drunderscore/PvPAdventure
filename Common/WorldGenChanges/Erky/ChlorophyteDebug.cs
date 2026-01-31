using PvPAdventure.Common.GameTimer;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.WorldGenChanges.Erky;

[Autoload(Side = ModSide.Client)]
internal sealed class ChlorophyteDebug : ModSystem
{
    private static int _scanX;
    private static int _scanY;

    private static int _scanOre;
    private static int _scanBrick;
    private static int _scanJungleGrass;
    private static int _scanMud;
    private static int _scanDeepJungleGrass;

    private static int _lastOre;
    private static int _lastBrick;
    private static int _lastJungleGrass;
    private static int _lastMud;
    private static int _lastDeepJungleGrass;

    public override void OnWorldLoad()
    {
        _scanX = 0;
        _scanY = 0;

        _scanOre = 0;
        _scanBrick = 0;
        _scanJungleGrass = 0;
        _scanMud = 0;
        _scanDeepJungleGrass = 0;

        _lastOre = 0;
        _lastBrick = 0;
        _lastJungleGrass = 0;
        _lastMud = 0;
        _lastDeepJungleGrass = 0;
    }

    public override void OnWorldUnload()
    {
        _scanX = 0;
        _scanY = 0;
    }

    public override void PostUpdateWorld()
    {
        if (Main.gameMenu)
            return;

        const int tilesPerTick = 6000;
        int scanned = 0;
        double deepGate = (Main.worldSurface + Main.rockLayer) / 2.0;

        while (scanned < tilesPerTick && _scanY < Main.maxTilesY)
        {
            Tile t = Main.tile[_scanX, _scanY];

            if (t.HasTile)
            {
                ushort type = t.TileType;

                if (type == TileID.Chlorophyte)
                    _scanOre++;
                else if (type == TileID.ChlorophyteBrick)
                    _scanBrick++;

                if (type == TileID.JungleGrass)
                {
                    _scanJungleGrass++;
                    if (_scanY > deepGate)
                        _scanDeepJungleGrass++;
                }
                else if (type == TileID.Mud)
                {
                    _scanMud++;
                }
            }

            _scanX++;

            if (_scanX >= Main.maxTilesX)
            {
                _scanX = 0;
                _scanY++;
            }

            scanned++;
        }

        if (_scanY >= Main.maxTilesY)
        {
            _lastOre = _scanOre;
            _lastBrick = _scanBrick;
            _lastJungleGrass = _scanJungleGrass;
            _lastMud = _scanMud;
            _lastDeepJungleGrass = _scanDeepJungleGrass;

            _scanX = 0;
            _scanY = 0;

            _scanOre = 0;
            _scanBrick = 0;
            _scanJungleGrass = 0;
            _scanMud = 0;
            _scanDeepJungleGrass = 0;
        }

        // Exactly once per minute by default.
        if (Main.GameUpdateCount % (60 * 60) != 0)
            return;

        bool isPlaying = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        bool isHardmode = Main.hardMode;
        bool oneMechDefeated = NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;
        bool canChloroSpawn = isPlaying && isHardmode && oneMechDefeated;

        if (!canChloroSpawn)
        {
            string reason = false switch
            {
                _ when !isPlaying => "game phase is not Playing",
                _ when !isHardmode => "world is not Hardmode",
                _ when !oneMechDefeated => "no mechanical boss defeated",
                _ => "unknown"
            };

            Log.Chat($"Chloro cannot spawn. Reason: {reason}");
            return;
        }

        var cfg = ModContent.GetInstance<ServerConfig>().WorldGeneration;

        int seedDen = cfg.ChlorophyteGrowChanceModifier;
        int spreadN = cfg.ChlorophyteSpreadChanceModifier;
        int limit = cfg.ChlorophyteGrowLimitModifier;

        float seedChance = 1f / seedDen;
        float spreadAttemptChance = spreadN == 1 ? 0f : (spreadN - 1f) / spreadN;

        long idx = (long)_scanY * Main.maxTilesX + _scanX;
        long total = (long)Main.maxTilesX * Main.maxTilesY;
        float scan = total <= 0 ? 0f : (float)idx / total;

        string msg =
            $"[ChloroDbg] ore={_lastOre} brick={_lastBrick} jungleGrass={_lastJungleGrass} deepJungleGrass={_lastDeepJungleGrass} mud={_lastMud} " +
            $"scan={scan:P0} deepGateY={deepGate:0} hardMode={Main.hardMode} " +
            $"seedDen={seedDen} seedChance={seedChance:P2} spreadN={spreadN} spreadAttemptChance={spreadAttemptChance:P0} limit={limit}";

        Log.Debug(msg);
        Log.Chat(msg);
    }
}
