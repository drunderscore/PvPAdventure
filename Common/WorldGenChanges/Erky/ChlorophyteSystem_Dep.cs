//using Mono.Cecil;
//using Mono.Cecil.Cil;
//using MonoMod.Cil;
//using PvPAdventure.Core.Config;
//using System;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.WorldGenChanges.Erky;

//internal sealed class ChlorophyteWorldGenSystem : ModSystem
//{
//    private static bool _seedPatched;
//    private static bool _spreadPatched;
//    private static bool _limitsPatched;

//    private static int _telemetryTimer;
//    private static int _scanX;
//    private static int _scanY;
//    private static int _scanOre;
//    private static int _scanBrick;
//    private static int _lastOre;
//    private static int _lastBrick;
//    private static int _scanDeepJungleGrass;
//    private static int _lastDeepJungleGrass;


//    // scan jungle
//    private static int _scanJungleGrass;
//    private static int _scanMud;
//    private static int _lastJungleGrass;
//    private static int _lastMud;


//    public override void Load()
//    {
//        IL_WorldGen.hardUpdateWorld += IL_hardUpdateWorld;
//        IL_WorldGen.Chlorophyte += IL_Chlorophyte;
//        Log.Debug("[Chloro] Subscribed IL_WorldGen.hardUpdateWorld + IL_WorldGen.Chlorophyte");
//    }

//    public override void Unload()
//    {
//        IL_WorldGen.hardUpdateWorld -= IL_hardUpdateWorld;
//        IL_WorldGen.Chlorophyte -= IL_Chlorophyte;
//    }

//    public override void OnWorldLoad()
//    {
//        _scanX = 0;
//        _scanY = 0;
//        _scanOre = 0;
//        _scanBrick = 0;
//        _scanJungleGrass = 0;
//        _scanMud = 0;

//        _lastOre = 0;
//        _lastBrick = 0;
//        _lastJungleGrass = 0;
//        _lastMud = 0;
//    }

//    public override void OnWorldUnload()
//    {
//        _telemetryTimer = 0;
//    }

//    public override void PostUpdateWorld()
//    {
//        if (Main.gameMenu)
//            return;

//        const int tilesPerTick = 6000;
//        int scanned = 0;
//        double deepGate = (Main.worldSurface + Main.rockLayer) / 2.0;
//        int scanDeepJungle = 0;

//        while (scanned < tilesPerTick && _scanY < Main.maxTilesY)
//        {
//            Tile t = Main.tile[_scanX, _scanY];

//            if (t.HasTile)
//            {
//                ushort type = t.TileType;

//                if (type == TileID.Chlorophyte)
//                    _scanOre++;
//                else if (type == TileID.ChlorophyteBrick)
//                    _scanBrick++;

//                if (type == TileID.JungleGrass)
//                {
//                    _scanJungleGrass++;
//                    if (_scanY > deepGate)
//                        scanDeepJungle++;
//                }
//                else if (type == TileID.Mud)
//                {
//                    _scanMud++;
//                }
//            }

//            _scanX++;

//            if (_scanX >= Main.maxTilesX)
//            {
//                _scanX = 0;
//                _scanY++;
//            }

//            scanned++;
//        }

//        if (_scanY >= Main.maxTilesY)
//        {
//            _lastOre = _scanOre;
//            _lastBrick = _scanBrick;
//            _lastJungleGrass = _scanJungleGrass;
//            _lastMud = _scanMud;

//            _scanX = 0;
//            _scanY = 0;
//            _scanOre = 0;
//            _scanBrick = 0;
//            _scanJungleGrass = 0;
//            _scanMud = 0;
//        }

//        if (Main.GameUpdateCount % 60 != 0)
//            return;

//        var cfg = ModContent.GetInstance<ServerConfig>().WorldGeneration;

//        int seedDen = cfg.ChlorophyteGrowChanceModifier < 1 ? 1 : cfg.ChlorophyteGrowChanceModifier;
//        int spreadN = cfg.ChlorophyteSpreadChanceModifier < 1 ? 1 : cfg.ChlorophyteSpreadChanceModifier;
//        int limit = cfg.ChlorophyteGrowLimitModifier < 1 ? 1 : cfg.ChlorophyteGrowLimitModifier;

//        float attemptChance = spreadN == 1 ? 0f : (spreadN - 1f) / spreadN;

//        long idx = (long)_scanY * Main.maxTilesX + _scanX;
//        long total = (long)Main.maxTilesX * Main.maxTilesY;
//        float scan = total <= 0 ? 0f : (float)idx / total;

//        string msg =
//            $"[Chloro] ore={_lastOre} brick={_lastBrick} jungleGrass={_lastJungleGrass} mud={_lastMud} deepGateY={deepGate:0} deepJungleGrass~={scanDeepJungle} " +
//            $"scan={scan:P0} seedDen={seedDen} spreadN={spreadN} attemptChance={attemptChance:P0} limit={limit} hardMode={Main.hardMode} " +
//            $"patched(seed={_seedPatched}, spread={_spreadPatched}, limits={_limitsPatched})";

//        Log.Debug(msg);

//        if (Main.netMode != NetmodeID.Server)
//            Log.Chat(msg);
//    }

//    private static void IL_hardUpdateWorld(ILContext il)
//    {
//        bool seedOk = false;
//        bool spreadOk = false;

//        try
//        {
//            var c = new ILCursor(il);

//            while (c.TryGotoNext(MoveType.Before, i =>
//            {
//                if (i.OpCode != OpCodes.Call && i.OpCode != OpCodes.Callvirt)
//                    return false;

//                if (i.Operand is not MethodReference mr)
//                    return false;

//                return mr.Name == "Next"
//                    && mr.Parameters.Count == 1
//                    && mr.Parameters[0].ParameterType.MetadataType == MetadataType.Int32
//                    && mr.ReturnType.MetadataType == MetadataType.Int32;
//            }))
//            {
//                int callIdx = c.Index;
//                if (callIdx < 1 || !il.Body.Instructions[callIdx - 1].MatchLdcI4(300))
//                {
//                    c.Index = callIdx + 1;
//                    continue;
//                }

//                c.Index = callIdx - 1;
//                c.Remove();
//                c.EmitDelegate(() =>
//                {
//                    int den = ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowChanceModifier;
//                    return den < 1 ? 1 : den;
//                });

//                seedOk = true;
//                break;
//            }

//            var ins = il.Body.Instructions;

//            for (int i = 1; i < ins.Count; i++)
//            {
//                Instruction call = ins[i];

//                if (call.OpCode != OpCodes.Call && call.OpCode != OpCodes.Callvirt)
//                    continue;

//                if (call.Operand is not MethodReference mr)
//                    continue;

//                if (mr.Name != "Next" || mr.Parameters.Count != 1 || mr.Parameters[0].ParameterType.MetadataType != MetadataType.Int32 || mr.ReturnType.MetadataType != MetadataType.Int32)
//                    continue;

//                // We only care about Next(3)
//                if (!ins[i - 1].MatchLdcI4(3))
//                    continue;

//                int start = Math.Max(0, i - 260);
//                int end = Math.Min(ins.Count - 1, i + 160);

//                bool sawChloro = false;
//                bool sawDirRoll = false;

//                for (int k = start; k <= end; k++)
//                {
//                    if (ins[k].MatchLdcI4(TileID.Chlorophyte) || ins[k].MatchLdcI4(TileID.ChlorophyteBrick))
//                        sawChloro = true;

//                    if (k >= 1)
//                    {
//                        Instruction maybeCall = ins[k];

//                        if ((maybeCall.OpCode == OpCodes.Call || maybeCall.OpCode == OpCodes.Callvirt)
//                            && maybeCall.Operand is MethodReference mr2
//                            && mr2.Name == "Next"
//                            && mr2.Parameters.Count == 1
//                            && mr2.Parameters[0].ParameterType.MetadataType == MetadataType.Int32
//                            && mr2.ReturnType.MetadataType == MetadataType.Int32
//                            && ins[k - 1].MatchLdcI4(4))
//                        {
//                            sawDirRoll = true;
//                        }
//                    }

//                    if (sawChloro && sawDirRoll)
//                        break;
//                }

//                if (!sawChloro || !sawDirRoll)
//                    continue;

//                // Replace the "3" argument directly.
//                var c2 = new ILCursor(il) { Index = i - 1 };
//                c2.Remove();
//                c2.EmitDelegate(() =>
//                {
//                    int n = ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteSpreadChanceModifier;
//                    return n < 1 ? 1 : n;
//                });

//                spreadOk = true;
//                break;
//            }

//        }
//        catch (Exception ex)
//        {
//            Log.Debug($"[Chloro] hardUpdateWorld patch exception: {ex}");
//        }

//        _seedPatched |= seedOk;
//        _spreadPatched |= spreadOk;
//        Log.Debug($"[Chloro] hardUpdateWorld patched(seed={seedOk}, spread={spreadOk}) totals(seed={_seedPatched}, spread={_spreadPatched})");
//    }

//    private static void IL_Chlorophyte(ILContext il)
//    {
//        bool a = false;
//        bool b = false;

//        try
//        {
//            var c = new ILCursor(il);

//            if (c.TryGotoNext(i => i.MatchLdcI4(40)))
//            {
//                c.Remove();
//                c.EmitDelegate(() =>
//                {
//                    int v = ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier;
//                    if (v < 1)
//                        v = 1;
//                    if (v > 999999)
//                        v = 999999;
//                    return v;
//                });

//                a = true;
//            }

//            c.Index = 0;

//            if (c.TryGotoNext(i => i.MatchLdcI4(130)))
//            {
//                c.Remove();
//                c.EmitDelegate(() =>
//                {
//                    int v = ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier;
//                    if (v < 1)
//                        v = 1;
//                    if (v > 999999)
//                        v = 999999;
//                    return v;
//                });

//                b = true;
//            }
//        }
//        catch (Exception ex)
//        {
//            Log.Debug($"[Chloro] Chlorophyte() patch exception: {ex}");
//        }

//        _limitsPatched |= a && b;

//        Log.Debug($"[Chloro] Chlorophyte() patched(40={a}, 130={b}) totals(limits={_limitsPatched})");
//    }
//}
