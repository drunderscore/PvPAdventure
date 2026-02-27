using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.MatchHistory;

/// <summary>
/// Provides static methods and storage for recording, saving, and loading PvP match results as .nbt files. Maintains an
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

        // Load legacy .json matches (pre 2026-02)
        matches.AddRange(LegacyMatchJsonStorage.LoadMatchesFromFolder(folderPath));

        // Sort by date, newest first
        matches.Sort((a, b) => b.Start.CompareTo(a.Start));
        return matches;
    }

    public static int GetLocalPlacement(MatchResult match)
    {
        Team localTeam = Team.None;
        foreach (var p in match.Players ?? [])
            if (p.SteamId == match.LocalSteamId) { localTeam = p.Team; break; }

        if (localTeam == Team.None) return 0;

        List<int> points = [];
        int localPoints = int.MinValue;
        foreach (var tp in match.TeamPoints ?? [])
        {
            if (tp.Team == Team.None) continue;
            points.Add(tp.Points);
            if (tp.Team == localTeam) localPoints = tp.Points;
        }

        if (localPoints == int.MinValue) return 0;

        points.Sort((a, b) => b.CompareTo(a));
        int rank = 1;
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0 && points[i] < points[i - 1]) rank = i + 1;
            if (points[i] == localPoints) return rank;
        }
        return 0;
    }

    public static int GetPlacementGems(int placement)
    {
        ReadOnlySpan<int> rewards = [50, 40, 30, 20, 10];
        return placement > 0 && placement <= rewards.Length ? rewards[placement - 1] : 0;
    }
}