// ┌─────────────────────────────────────────────────────────────────────┐
// │  THE ONLY FILE TO EDIT when adding a new stat group.                │
// │                                                                     │
// │  1. Add a yield return line in AllGroups()                          │
// │  2. Add a static BuildXxx() method below                            │
// └─────────────────────────────────────────────────────────────────────┘

using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal readonly record struct DebugStatGroup(string Header, Color Color, Func<string[]> BuildRows);

internal static class DebugStats
{
    // ── ADD NEW GROUPS HERE ───────────────────────────────────────────
    internal static IEnumerable<DebugStatGroup> AllGroups()
    {
        yield return new("Chlorophyte", new Color(100, 220, 100), BuildChlorophyte);
        yield return new("Fishing", new Color(100, 180, 240), BuildFishing);
    }

    // ── ROW BUILDERS — add yours below ───────────────────────────────
    static string[] BuildChlorophyte()
    {
        bool isPlaying = ModContent.GetInstance<GameManager>()?.CurrentPhase == GameManager.Phase.Playing;
        bool isHardmode = Main.hardMode;
        bool mechDefeated = NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;
        bool planteraDefeated = NPC.downedPlantBoss;
        bool canSpawn = isPlaying && isHardmode && mechDefeated;

        string spawnStatus = canSpawn ? "yes" : $"no ({(
            !isPlaying ? "not Playing" :
            !isHardmode ? "not Hardmode" :
                            "no mech boss")})";

        var cfg = ModContent.GetInstance<ServerConfig>()?.WorldGeneration;
        int seedDen = cfg?.ChlorophyteGrowChanceModifier ?? 0;
        int spreadN = cfg?.ChlorophyteSpreadChanceModifier ?? 0;
        int limit = cfg?.ChlorophyteGrowLimitModifier ?? 0;

        float seedChance = seedDen > 0 ? 1f / seedDen : 0f;
        float spreadChance = spreadN > 1 ? (spreadN - 1f) / spreadN : 0f;

        return [
            $"Can spawn: {spawnStatus}",
            $"Hardmode: {isHardmode}",
            $"Mech defeated: {mechDefeated}",
            $"Plantera defeated: {planteraDefeated}",
            $"Ore (last scan): {DebugChlorophyteScan.LastOre}",
            $"Brick (last scan): {DebugChlorophyteScan.LastBrick}",
            $"Deep jungle grass: {DebugChlorophyteScan.LastDeepJungleGrass}",
            $"Mud: {DebugChlorophyteScan.LastMud}",
            $"Scan progress: {DebugChlorophyteScan.ScanProgress:P0}",
            $"Seed chance: {seedChance:P2} (1/{seedDen})",
            $"Spread chance: {spreadChance:P0}",
            $"Limit modifier: {limit}",
        ];
    }

    static string[] BuildFishing()
    {
        Player local = Main.LocalPlayer;
        int moonPhase = Main.moonPhase; // 0 = full moon (best), 4 = new moon (worst)
        string moonLabel = moonPhase switch { 0 => "Full (best)", 4 => "New (worst)", _ => moonPhase.ToString() };

        double timeHours = Main.time / 3600.0;
        string timeLabel = Main.dayTime
            ? $"Day {timeHours:F1}h"
            : $"Night {timeHours:F1}h";

        // Find best bait power in inventory
        int bestBait = 0;
        for (int i = 0; i < 58; i++)
        {
            Item item = local.inventory[i];
            if (item.bait > bestBait)
                bestBait = item.bait;
        }

        return [
            $"Fishing power: {local.fishingSkill}",
            $"Best bait: {(bestBait > 0 ? $"{bestBait}%" : "none")}",
            $"Raining: {Main.raining}",
            $"Time: {timeLabel}",
            $"Moon phase: {moonLabel}",
            $"Quest done today: {Main.anglerQuestFinished}",
            $"Sonar potion: {local.sonarPotion}",
            $"Crate potion: {local.cratePotion}",
        ];
    }
}
#endif