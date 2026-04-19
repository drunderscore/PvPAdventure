using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.Achievements;

public sealed class AchievementProgress
{
    /// <summary>
    /// Maps the progress of an achievement by its ID.
    /// The value represents the current progress toward the achievement's target. 
    /// For example, if an achievement requires 100 kills and the player has 30 kills, the value would be 30.
    /// </summary>
    public Dictionary<string, int> Progress { get; set; } = [];

    public HashSet<string> Collected { get; set; } = [];

    public bool IsCollected(string id)
    {
        return Collected.Contains(id);
    }

    public bool TryCollect(string id, int target)
    {
        target = Math.Max(target, 1);

        int progress = Get(id);
        if (progress < target)
            return false;

        return Collected.Add(id);
    }

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
        HashSet<string> allowed = [];

        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];
            allowed.Add(id);

            int target = Math.Max(def.Target, 1);
            int v = Get(id);

            if (v < 0)
                v = 0;

            if (v > target)
                v = target;

            Progress[id] = v;

            if (v < target)
                Collected.Remove(id);
        }

        Collected.RemoveWhere(x => !allowed.Contains(x));
    }

    public TagCompound ToTag()
    {
        TagCompound prog = new();
        foreach (var kv in Progress)
            prog[kv.Key] = kv.Value;

        List<string> collected = [.. Collected];

        return new TagCompound
        {
            ["Progress"] = prog,
            ["Collected"] = collected
        };
    }

    public static AchievementProgress FromTag(TagCompound tag)
    {
        AchievementProgress data = new();

        if (tag.ContainsKey("Progress"))
        {
            TagCompound prog = tag.Get<TagCompound>("Progress");
            foreach (var kv in prog)
                data.Progress[kv.Key] = prog.GetInt(kv.Key);
        }

        if (tag.ContainsKey("Collected"))
        {
            var list = tag.GetList<string>("Collected");
            for (int i = 0; i < list.Count; i++)
                data.Collected.Add(list[i]);
        }

        return data;
    }
}