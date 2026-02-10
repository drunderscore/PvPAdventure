using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MatchHistory.Achievements;

public static class AchievementStorage
{
    private static string GetFilePath()
    {
        return Path.Combine(Main.SavePath, "PvPAdventure", "Achievements", "Achievements.nbt");
    }

    public static AchievementData Data { get; private set; } = new();

    public static void Load()
    {
        if (Main.dedServ)
            return;

        try
        {
            string path = GetFilePath();

            if (File.Exists(path))
                Data = AchievementData.FromTag(TagIO.FromFile(path));
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

        Data = new();

        for (int i = 0; i < matches.Count; i++)
            ApplyMatch(matches[i]);

        Data.Sanitize();
        Save();
    }
}

public sealed class AchievementData
{
    public Dictionary<string, int> Progress { get; set; } = [];

    public int Get(string id)
    {
        if (Progress.TryGetValue(id, out int v))
            return v;

        return 0;
    }

    public bool Add(string id, int target, int delta)
    {
        if (delta <= 0)
            return false;

        target = Math.Max(target, 1);

        Progress.TryGetValue(id, out int before);
        if (before >= target)
            return false;

        int after = before + delta;
        if (after > target)
            after = target;

        if (after == before)
            return false;

        Progress[id] = after;
        return true;
    }

    public void Sanitize()
    {
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];

            int target = Math.Max(def.Target, 1);
            int v = Get(id);

            if (v < 0) v = 0;
            if (v > target) v = target;

            Progress[id] = v;
        }
    }

    public TagCompound ToTag()
    {
        TagCompound prog = new();
        foreach (var kv in Progress)
            prog[kv.Key] = kv.Value;

        return new TagCompound
        {
            ["Progress"] = prog
        };
    }

    public static AchievementData FromTag(TagCompound tag)
    {
        AchievementData data = new();

        if (tag.ContainsKey("Progress"))
        {
            TagCompound prog = tag.Get<TagCompound>("Progress");
            foreach (var kv in prog)
                data.Progress[kv.Key] = prog.GetInt(kv.Key);
        }

        return data;
    }
}

