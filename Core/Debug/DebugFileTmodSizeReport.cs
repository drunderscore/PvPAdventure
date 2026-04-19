using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal class DebugFileTmodSizeReport : ModSystem
{
    public override void Load()
    {
        string path = @"C:\Users\erikm\Documents\My Games\Terraria\tModLoader\ModReader\PvPAdventure";
        DebugFileTmodSizeReport.PrintReport(path);
    }

    private readonly record struct FileEntry(string RelativePath, long RawBytes, long EstimatedCompressedBytes);
    private readonly record struct FolderEntry(string RelativePath, long RawBytes, long EstimatedCompressedBytes);

    public static void PrintReport(string extractedRoot, int topFileCount = 40)
    {
        if (string.IsNullOrWhiteSpace(extractedRoot))
        {
            Log("No extracted root path was provided.");
            return;
        }

        string root = Path.GetFullPath(extractedRoot);

        if (!Directory.Exists(root))
        {
            Log($"Folder not found: {root}");
            return;
        }

        List<FileEntry> files = GetFiles(root);
        List<FolderEntry> folders = GetFolders(root, files);

        Log("Extracted tmod size report");
        Log("==================================================");
        Log($" {FormatSize(folders[0].RawBytes),10} raw   {FormatSize(folders[0].EstimatedCompressedBytes),10} est   <root>");

        foreach (FolderEntry folder in folders.Skip(1).OrderByDescending(x => x.RawBytes).ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase))
            Log($" {FormatSize(folder.RawBytes),10} raw   {FormatSize(folder.EstimatedCompressedBytes),10} est   {folder.RelativePath}");

        Log("");
        Log($"Largest {Math.Min(topFileCount, files.Count)} files by estimated compressed cost");
        Log("==================================================");

        foreach (FileEntry file in files.OrderByDescending(x => x.EstimatedCompressedBytes).ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase).Take(topFileCount))
            Log($" {FormatSize(file.EstimatedCompressedBytes),10} est   {FormatSize(file.RawBytes),10} raw   {file.RelativePath}");

        Log("");
        Log("Totals by extension");
        Log("==================================================");

        foreach (var extensionGroup in files
            .GroupBy(x => Path.GetExtension(x.RelativePath).ToLowerInvariant())
            .Select(x => new
            {
                Extension = string.IsNullOrEmpty(x.Key) ? "<no ext>" : x.Key,
                RawBytes = x.Sum(f => f.RawBytes),
                EstimatedCompressedBytes = x.Sum(f => f.EstimatedCompressedBytes),
                Count = x.Count()
            })
            .OrderByDescending(x => x.EstimatedCompressedBytes)
            .ThenBy(x => x.Extension, StringComparer.OrdinalIgnoreCase))
        {
            Log($" {FormatSize(extensionGroup.RawBytes),10} raw   {FormatSize(extensionGroup.EstimatedCompressedBytes),10} est   {extensionGroup.Count,5} files   {extensionGroup.Extension}");
        }
    }

    private static List<FileEntry> GetFiles(string root)
    {
        List<FileEntry> files = [];

        foreach (string filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            FileInfo info = new(filePath);
            string relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            long rawBytes = info.Length;
            long estimatedCompressedBytes = EstimateCompressedSize(filePath);

            files.Add(new FileEntry(relativePath, rawBytes, estimatedCompressedBytes));
        }

        return files;
    }

    private static List<FolderEntry> GetFolders(string root, List<FileEntry> files)
    {
        Dictionary<string, (long RawBytes, long EstimatedCompressedBytes)> totals = new(StringComparer.OrdinalIgnoreCase)
        {
            ["<root>"] = (0, 0)
        };

        foreach (FileEntry file in files)
        {
            AddToFolder(totals, "<root>", file.RawBytes, file.EstimatedCompressedBytes);

            string[] parts = file.RelativePath.Split('/');
            if (parts.Length <= 1)
                continue;

            string current = parts[0];
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (i > 0)
                    current += "/" + parts[i];

                AddToFolder(totals, current, file.RawBytes, file.EstimatedCompressedBytes);
            }
        }

        List<FolderEntry> folders =
        [
            .. totals.Select(x => new FolderEntry(x.Key, x.Value.RawBytes, x.Value.EstimatedCompressedBytes))
        ];

        folders.Sort(static (a, b) =>
        {
            int rootOrder = a.RelativePath == "<root>" ? -1 : b.RelativePath == "<root>" ? 1 : 0;
            if (rootOrder != 0)
                return rootOrder;

            int sizeOrder = b.RawBytes.CompareTo(a.RawBytes);
            if (sizeOrder != 0)
                return sizeOrder;

            return string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase);
        });

        return folders;
    }

    private static void AddToFolder(Dictionary<string, (long RawBytes, long EstimatedCompressedBytes)> totals, string folder, long rawBytes, long estimatedCompressedBytes)
    {
        if (!totals.TryGetValue(folder, out var existing))
            existing = (0, 0);

        totals[folder] = (existing.RawBytes + rawBytes, existing.EstimatedCompressedBytes + estimatedCompressedBytes);
    }

    private static long EstimateCompressedSize(string filePath)
    {
        using FileStream input = File.OpenRead(filePath);
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.SmallestSize, leaveOpen: true))
            input.CopyTo(gzip);

        return output.Length;
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024d && unitIndex < units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        if (unitIndex == 0)
            return $"{bytes} B";

        return $"{size:0.##} {units[unitIndex]}";
    }

    private static void Log(string text)
    {
        ModContent.GetInstance<PvPAdventure>().Logger.Debug(text);
    }
}
#endif
