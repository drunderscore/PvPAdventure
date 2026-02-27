using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

/// <summary>
/// Legacy matches for players, saved somewhere between 2026-01 and 2026-02
/// Saved in PvPAdventure/MatchHistory/{date}.json
/// In the future we use .nbt and award TPVPA gems, but these old legacy games should still be viewable for players.
/// </summary>
public static class LegacyMatchJsonStorage
{
    public static readonly List<MatchResult> Matches = [];

    public static string GetFolderPath() => Path.Combine(Main.SavePath, "PvPAdventure", "MatchHistory");

    public static List<MatchResult> LoadMatchesFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        // Use same options as saving to ensure enums deserialize correctly
        JsonSerializerOptions Opt = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Load all .json files in the folder
        List<MatchResult> matches = [];
        foreach (string path in Directory.EnumerateFiles(folderPath, "*.json"))
        {
            string json = File.ReadAllText(path);
            MatchResult match = JsonSerializer.Deserialize<MatchResult>(json, Opt);
            matches.Add(match);
        }

        // Sort by date, newest first
        matches.Sort((a, b) => b.Start.CompareTo(a.Start));
        return matches;
    }

    //private static readonly JsonSerializerOptions JsonOptions = new()
    //{
    //    WriteIndented = true,
    //    Converters =
    //    {
    //        new JsonStringEnumConverter()
    //    }
    //};

    //[Obsolete("Use LoadMatchesFormFolder instead")]
    //public static void LoadAllFromDisk()
    //{
    //    try
    //    {
    //        string dir = GetFolderPath();

    //        if (!Directory.Exists(dir))
    //        {
    //            Log.Error("No match history directory found");
    //            return;
    //        }

    //        Matches.Clear();

    //        var jsonFiles = Directory.GetFiles(dir, "*.json");

    //        foreach (var file in jsonFiles)
    //        {
    //            try
    //            {
    //                string json = File.ReadAllText(file);
    //                var match = JsonSerializer.Deserialize<MatchResult>(json, JsonOptions);
    //                Matches.Add(match);
    //            }
    //            catch (Exception e)
    //            {
    //                Log.Error($"Failed to load match from {Path.GetFileName(file)}: {e}");
    //            }
    //        }

    //        // Sort by date, newest first
    //        Matches.Sort((a, b) => b.Start.CompareTo(a.Start));

    //        Log.Info($"Loaded {Matches.Count} matches from disk");
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to load match history: {e}");
    //    }
    //}

    //#region Generate filename
    //public static string GetMatchFilePath(DateTime matchStartUtc)
    //{
    //    string dir = GetFolderPath();
    //    return Path.Combine(dir, GenerateFilename(matchStartUtc));
    //}
    //private static string GenerateFilename(DateTime matchStart)
    //{
    //    // Convert UTC to local time for filename
    //    DateTime local = matchStart.ToLocalTime();

    //    // Format: 2026-02-05_03;16.json
    //    return $"{local:yyyy-MM-dd_HH;mm}.json";
    //}
    //#endregion

    #region Record a match result to disk as JSON
    //public static void RecordAndSave(MatchResult match)
    //{
    //    if (Main.dedServ)
    //    {
    //        return;
    //    }

    //    Matches.Insert(0, match);
    //    SaveMatchToDisk(match);
    //}

    //private static void SaveMatchToDisk(MatchResult match)
    //{
    //    try
    //    {
    //        string dir = GetFolderPath();
    //        Directory.CreateDirectory(dir);

    //        // Generate filename: 2026-02-05_03;16.json
    //        string filename = GenerateFilename(match.Start);
    //        string path = Path.Combine(dir, filename);

    //        string json = JsonSerializer.Serialize(match, JsonOptions);

    //        // Write with atomic file replacement to avoid corruption
    //        string tmp = path + ".tmp";
    //        File.WriteAllText(tmp, json);

    //        if (File.Exists(path))
    //        {
    //            File.Replace(tmp, path, null);
    //        }
    //        else
    //        {
    //            File.Move(tmp, path);
    //        }

    //        Log.Info($"Saved match to {filename}");
    //        Log.Chat($"Saved match to {filename}");
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to save match history: {e}");
    //    }
    //}
    #endregion
}
