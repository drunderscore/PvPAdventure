using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

/// <summary>
/// Provides static methods and storage for recording, saving, and loading PvP match results as JSON files. Maintains an
/// in-memory list of match history and handles persistence to disk.
/// </summary>
public static class MatchStorage
{
    public static readonly List<MatchResult> Matches = [];

    public static string GetFolderPath() => Path.Combine(Main.SavePath, "PvPAdventure", "MatchHistory");

    public static string GetMatchFilePath(DateTime matchStartUtc)
    {
        string dir = GetFolderPath();
        return Path.Combine(dir, GenerateFilename(matchStartUtc));
    }

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

            // Generate filename: 2026-02-05_03;16.nbt
            string filename = GenerateFilename(match.Start);
            string path = Path.Combine(dir, filename);

            TagCompound tag = match.ToTag();

            string tmp = path + ".tmp";

            using (FileStream fs = new(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                TagIO.ToStream(tag, fs, compress: true);

            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);

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

        // Format: 2026-02-05_03;16.nbt
        return $"{local:yyyy-MM-dd_HH;mm}.nbt";
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

            foreach (string file in Directory.EnumerateFiles(dir, "*.nbt"))
            {
                try
                {
                    TagCompound tag = TagIO.FromFile(file, compressed: true);
                    MatchResult match = MatchResult.FromTag(tag);
                    Matches.Add(match);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load match from {Path.GetFileName(file)}: {e}");
                }
            }

            Matches.Sort((a, b) => b.Start.CompareTo(a.Start));
            Log.Info($"Loaded {Matches.Count} matches from disk");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load match history: {e}");
        }
    }

    public static List<MatchResult> LoadMatchesFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        List<MatchResult> matches = [];

        foreach (string path in Directory.EnumerateFiles(folderPath, "*.nbt"))
        {
            try
            {
                TagCompound tag = TagIO.FromFile(path, compressed: true);
                MatchResult match = MatchResult.FromTag(tag);
                matches.Add(match);
            }
            catch
            {
                Log.Error("Error: Failed to load matches from folder: " + folderPath);
            }
        }

        // Sort by date, newest first
        matches.Sort((a, b) => b.Start.CompareTo(a.Start));
        return matches;
    }
}