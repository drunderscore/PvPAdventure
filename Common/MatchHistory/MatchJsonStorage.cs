using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;

namespace PvPAdventure.Common.MatchHistory;

public static class MatchJsonStorage
{
    public static readonly List<MatchResult> Matches = [];

    private static string GetFolderPath() => Path.Combine(Main.SavePath, "PvPAdventure", "MatchHistory");

    public static string GetMatchFilePath(DateTime matchStartUtc)
    {
        string dir = GetFolderPath();
        return Path.Combine(dir, GenerateFilename(matchStartUtc));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static void RecordAndSave(MatchResult match)
    {
        if (Main.dedServ)
        {
            return;
        }

        Matches.Insert(0, match);
        SaveMatchToDisk(match);
    }

    private static void SaveMatchToDisk(MatchResult match)
    {
        try
        {
            string dir = GetFolderPath();
            Directory.CreateDirectory(dir);

            // Generate filename: 2026-02-05_03;16.json
            string filename = GenerateFilename(match.Start);
            string path = Path.Combine(dir, filename);

            string json = JsonSerializer.Serialize(match, JsonOptions);

            // Write with atomic file replacement to avoid corruption
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(path))
            {
                File.Replace(tmp, path, null);
            }
            else
            {
                File.Move(tmp, path);
            }

            Log.Info($"Saved match to {filename}");
            Log.Chat($"Saved match to {filename}");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save match history: {e}");
        }
    }

    private static string GenerateFilename(DateTime matchStart)
    {
        // Convert UTC to local time for filename
        DateTime local = matchStart.ToLocalTime();

        // Format: 2026-02-05_03;16.json
        return $"{local:yyyy-MM-dd_HH;mm}.json";
    }

    public static void LoadAllFromDisk()
    {
        try
        {
            string dir = GetFolderPath();

            if (!Directory.Exists(dir))
            {
                Log.Info("No match history directory found");
                return;
            }

            Matches.Clear();

            var jsonFiles = Directory.GetFiles(dir, "*.json");

            foreach (var file in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var match = JsonSerializer.Deserialize<MatchResult>(json, JsonOptions);
                    Matches.Add(match);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load match from {Path.GetFileName(file)}: {e}");
                }
            }

            // Sort by date, newest first
            Matches.Sort((a, b) => b.Start.CompareTo(a.Start));

            Log.Info($"Loaded {Matches.Count} matches from disk");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load match history: {e}");
        }
    }
}