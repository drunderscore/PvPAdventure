using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria.Enums;

namespace PvPAdventure.Common.MatchHistory;
public static class MatchJsonLoader
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static List<MatchResult> LoadMatchesFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        List<MatchResult> matches = [];

        foreach (string path in Directory.EnumerateFiles(folderPath, "*.json"))
        {
            string json = File.ReadAllText(path);
            MatchResult match = JsonSerializer.Deserialize<MatchResult>(json, Opt);
            matches.Add(match);
        }

        matches.Sort((a, b) => b.Start.CompareTo(a.Start));
        return matches;
    }
}