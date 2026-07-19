//using PvPAdventure.Common.Statistics;
//using PvPHub.Common.MainMenu.API;
//using PvPHub.Common.MainMenu.API.Achievements;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Terraria;
//using Terraria.Enums;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Game.GameReporters;

///// <summary>
///// Posts PvPAdventure game events to PvPHub's achievement progress API.
///// </summary>
//[JITWhenModsEnabled("PvPHub")]
//[ExtendsFromMod("PvPHub")]
//internal static class AchievementReporter
//{
//    private const string GameMode = "pvpa";

//    /// <summary>
//    /// Call when a single Sniper Rifle projectile confirms its second player hit.
//    /// See <see cref="SniperMultiHitTracker"/> for the projectile-side hook.
//    /// </summary>
//    public static void OnSniperDoubleHit(Player shooter)
//    {
//        if (!PvPHubCompat.IsPvPHubLoaded)
//            return;

//        if (!PvPHubCompat.TryGetSteamId(shooter, out ulong steamId))
//            return;

//        _ = ReportAsync(steamId, "hit_two_one_sniper_shot");
//    }

//    private static void ReportAll(ulong steamId, IEnumerable<string> achievementNames)
//    {
//        foreach (string achievementName in achievementNames)
//            _ = ReportAsync(steamId, achievementName);
//    }

//    private static async Task ReportAsync(ulong steamId, string achievementName)
//    {
//        try
//        {
//            AchievementRef achievement = new(achievementName, GameMode);
//            ApiResult<ApiAchievement> result =
//                await AchievementsApi.ProgressAchievementAsync(steamId, achievement, delta: 1).ConfigureAwait(false);

//            if (result.IsSuccess)
//                Log.Info($"'{achievementName}' → {steamId}  progress={result.Data.Progress}/{result.Data.Target}");
//            else
//                Log.Warn($"'{achievementName}' → {steamId}  failed: {result.ErrorMessage}");
//        }
//        catch (Exception ex)
//        {
//            Log.Error($"Unexpected error reporting '{achievementName}' for {steamId}: {ex}");
//        }
//    }
//}

