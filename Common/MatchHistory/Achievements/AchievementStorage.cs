using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;

namespace PvPAdventure.Common.MatchHistory.Achievements;

/// <summary>
/// Stores/loads local client achievement progress in:
/// Documents/My Games/Terraria/tModLoader/PvPAdventure/Achievements/Achievements.json
/// </summary>
public static class AchievementStorage
{
    private static string GetFolderPath() => Path.Combine(Main.SavePath, "PvPAdventure", "Achievements");
    private static string GetFilePath() => Path.Combine(GetFolderPath(), "Achievements.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AchievementSaveData Data { get; private set; } = AchievementSaveData.CreateDefault();

    public static void Load()
    {
        if (Main.dedServ)
            return;

        try
        {
            string path = GetFilePath();
            if (!File.Exists(path))
            {
                Data = AchievementSaveData.CreateDefault();
                Save(); // write initial file
                return;
            }

            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AchievementSaveData>(json, JsonOptions);

            Data = loaded ?? AchievementSaveData.CreateDefault();
            Data.Sanitize();
        }
        catch (Exception e)
        {
            // If you prefer "can't happen -> crash", rethrow here.
            // For safety, keep a default so the UI doesn't break.
            Data = AchievementSaveData.CreateDefault();
            Log.Error($"Failed to load Achievements.json: {e}");
        }
    }

    public static void RebuildFromMatches(IReadOnlyList<MatchResult> matches)
    {
        if (Main.dedServ)
            return;

        Data = AchievementSaveData.CreateDefault();

        for (int i = 0; i < matches.Count; i++)
        {
            MatchResult match = matches[i];

            var me = FindLocalPlayer(match);
            if (me == null)
                continue;

            Data.Increment(AchievementId.TotalKills100, me.Value.Kills);

            int teamPoints = FindTeamPoints(match, me.Value.Team);
            Data.Increment(AchievementId.TotalTeamPoints100, teamPoints);
        }

        Save();
    }

    public static void Save()
    {
        if (Main.dedServ)
            return;

        try
        {
            string dir = GetFolderPath();
            Directory.CreateDirectory(dir);

            string path = GetFilePath();
            string json = JsonSerializer.Serialize(Data, JsonOptions);

            // atomic write
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save Achievements.json: {e}");
        }
    }

    /// <summary>
    /// Apply one MatchResult into progress (local player only), then saves if anything changed.
    /// </summary>
    public static void ApplyMatch(MatchResult match)
    {
        if (Main.dedServ)
            return;

        var me = FindLocalPlayer(match);
        if (me == null)
            return;

        bool changed = false;

        // 1) 100 kills
        changed |= Data.Increment(AchievementId.TotalKills100, me.Value.Kills);

        // 2) 100 total points
        int teamPoints = FindTeamPoints(match, me.Value.Team);
        changed |= Data.Increment(AchievementId.TotalTeamPoints100, teamPoints);

        if (changed)
            Save();
    }

    private static (Terraria.Enums.Team Team, int Kills, int Deaths)? FindLocalPlayer(MatchResult match)
    {
        ulong id = (ulong)match.LocalSteamId;
        if (id == 0)
            return null;

        var players = match.Players ?? [];
        for (int i = 0; i < players.Length; i++)
        {
            if ((ulong)players[i].SteamId == id)
                return (players[i].Team, players[i].Kills, players[i].Deaths);
        }

        return null;
    }

    private static int FindTeamPoints(MatchResult match, Terraria.Enums.Team team)
    {
        var pts = match.TeamPoints ?? [];
        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i].Team == team)
                return pts[i].Points;
        }

        return 0;
    }
}

public enum AchievementId
{
    TotalKills100,
    TotalTeamPoints100
}

public sealed class AchievementSaveData
{
    public Dictionary<AchievementId, AchievementProgress> Achievements { get; set; } = new();

    public static AchievementSaveData CreateDefault()
    {
        var d = new AchievementSaveData();

        d.Achievements[AchievementId.TotalKills100] = new AchievementProgress
        {
            Name = "Get 1000 kills",
            Target = 1000,
            Current = 0,
            CompletedUtc = null
        };

        d.Achievements[AchievementId.TotalTeamPoints100] = new AchievementProgress
        {
            Name = "Get 1000 points",
            Target = 1000,
            Current = 0,
            CompletedUtc = null
        };

        return d;
    }

    public void Sanitize()
    {
        // Ensure all known achievements exist.
        var defaults = CreateDefault();
        foreach (var kv in defaults.Achievements)
        {
            if (!Achievements.TryGetValue(kv.Key, out var p) || p == null)
                Achievements[kv.Key] = kv.Value;
        }

        // Clamp and auto-complete.
        foreach (var kv in Achievements)
        {
            kv.Value.Sanitize();
        }
    }

    /// <summary>
    /// Adds delta progress to an achievement; returns true if modified.
    /// </summary>
    public bool Increment(AchievementId id, int delta)
    {
        if (delta <= 0)
            return false;

        if (!Achievements.TryGetValue(id, out var p) || p == null)
        {
            // Unknown -> create default bucket
            Achievements[id] = p = new AchievementProgress
            {
                Name = id.ToString(),
                Target = 100,
                Current = 0
            };
        }

        if (p.CompletedUtc != null)
            return false;

        int before = p.Current;
        p.Current += delta;

        if (p.Current >= p.Target)
        {
            p.Current = p.Target;
            p.CompletedUtc = DateTime.UtcNow;
        }

        return p.Current != before || (before < p.Target && p.CompletedUtc != null);
    }
}

public sealed class AchievementProgress
{
    public string Name { get; set; } = "";
    public int Target { get; set; }
    public int Current { get; set; }
    public DateTime? CompletedUtc { get; set; }

    public void Sanitize()
    {
        if (Target < 1)
            Target = 1;

        if (Current < 0)
            Current = 0;

        if (Current >= Target)
        {
            Current = Target;
            CompletedUtc ??= DateTime.UtcNow;
        }
    }
}
