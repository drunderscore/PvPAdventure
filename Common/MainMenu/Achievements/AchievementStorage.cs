using PvPAdventure.Common.MainMenu.Gems;
using PvPAdventure.Common.MainMenu.MatchHistory;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.Achievements;

public static class AchievementStorage
{
    private static string GetFilePath()
    {
        return Path.Combine(Main.SavePath, "PvPAdventure", "Achievements.nbt");
    }

    public static AchievementProgress Data { get; private set; } = new();

    public static void Load()
    {
        if (Main.dedServ)
            return;

        try
        {
            string path = GetFilePath();

            if (File.Exists(path))
                Data = AchievementProgress.FromTag(TagIO.FromFile(path));
            else
                Data = new();

            Data.Sanitize();
        }
        catch (Exception e)
        {
            Data = new();
            Log.Error($"Failed to load Achievements.nbt: {e}");
        }
    }

    public static void Save()
    {
        if (Main.dedServ)
            return;

        try
        {
            string path = GetFilePath();
            string? dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            TagIO.ToFile(Data.ToTag(), path);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save Achievements.nbt: {e}");
        }
    }

    public static bool TryCollect(string achievementId)
    {
        if (Main.dedServ)
            return false;

        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];
            if (id != achievementId)
                continue;

            if (!Data.TryCollect(id, def.Target))
                return false;

            GemStorage.Add(def.GemsReward);
            Save();
            return true;
        }

        return false;
    }

    public static void ApplyMatch(MatchResult match)
    {
        if (Main.dedServ)
            return;

        bool changed = false;

        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];
            changed |= Data.Add(id, def.Target, def.Delta(match));
        }

        if (changed)
            Save();
    }

    public static void RebuildFromMatches(IReadOnlyList<MatchResult> matches)
    {
        if (Main.dedServ)
            return;

        HashSet<string> collected = [.. Data.Collected];

        Data = new AchievementProgress
        {
            Collected = collected
        };

        for (int i = 0; i < matches.Count; i++)
            ApplyMatch(matches[i]);

        Data.Sanitize();
        Save();
    }
}
