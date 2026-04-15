using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
public class AssetSizeLoggerSystem : ModSystem
{
    public override void PostSetupContent()
    {
        if (Main.dedServ) return;
        try { LogTmodSizes(); }
        catch (Exception ex) { Mod.Logger.Debug($"[AssetSizeLogger] Failed: {ex}"); }
    }

    private void LogTmodSizes()
    {
        string tmodPath = Path.Combine(
            Main.SavePath, "Mods", $"{Mod.Name}.tmod");

        if (!File.Exists(tmodPath))
        {
            Mod.Logger.Debug("[AssetSizeLogger] .tmod not found at: " + tmodPath);
            return;
        }

        long tmodFileSize = new FileInfo(tmodPath).Length;
        var entries = ReadTmodFileTable(tmodPath);

        // Per-folder compressed totals
        var folderCompressed = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var folderUncompressed = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            string folder = GetFolder(e.Name);
            AccumulateFolders(folderCompressed, folder, e.CompressedSize);
            AccumulateFolders(folderUncompressed, folder, e.UncompressedSize);
        }

        Mod.Logger.Debug("==================================================");
        Mod.Logger.Debug($"[{Mod.Name}] .tmod total size: {FormatBytes(tmodFileSize)}");
        Mod.Logger.Debug($"[{Mod.Name}] File count: {entries.Count}");
        Mod.Logger.Debug("==================================================");
        Mod.Logger.Debug($"{"Compressed",12}  {"Uncompressed",14}  {"Ratio",7}  Folder");
        Mod.Logger.Debug("==================================================");

        foreach (var key in folderCompressed.Keys.OrderByDescending(k => folderCompressed[k]))
        {
            long comp = folderCompressed[key];
            long uncomp = folderUncompressed.GetValueOrDefault(key);
            float ratio = uncomp > 0 ? (float)comp / uncomp : 1f;
            string label = string.IsNullOrEmpty(key) ? "<root>" : key;
            Mod.Logger.Debug($"{FormatBytes(comp),12}  {FormatBytes(uncomp),14}  {ratio,6:P0}  {label}");
        }

        Mod.Logger.Debug("==================================================");
        Mod.Logger.Debug($"[{Mod.Name}] Top 60 biggest entries (compressed)");
        Mod.Logger.Debug("==================================================");

        int rank = 1;
        foreach (var e in entries.OrderByDescending(x => x.CompressedSize).Take(60))
        {
            float ratio = e.UncompressedSize > 0 ? (float)e.CompressedSize / e.UncompressedSize : 1f;
            Mod.Logger.Debug(
                $"{rank++,3}. {FormatBytes(e.CompressedSize),10}  raw: {FormatBytes(e.UncompressedSize),10}  ({ratio:P0})  {e.Name}");
        }

        Mod.Logger.Debug("==================================================");
    }

    // -----------------------------------------------------------------------
    //  .tmod binary format:
    //    "TMOD"           4 bytes  magic
    //    tML version      string   (7-bit-length-prefixed UTF8)
    //    hash             20 bytes
    //    signature        256 bytes
    //    dataLength       uint32   (length of the deflate block that follows)
    //    [deflate stream]
    //      modName        string
    //      modVersion     string
    //      fileCount      int32
    //      for each file:
    //        name         string
    //        uncompSize   int32
    //        compSize     int32
    //      [raw file data follows — we stop after the table]
    // -----------------------------------------------------------------------
    private static List<TmodEntry> ReadTmodFileTable(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        // Magic
        string magic = new(br.ReadChars(4));
        if (magic != "TMOD")
            throw new InvalidDataException("Not a .tmod file");

        br.ReadString();        // tML version
        br.ReadBytes(20);       // hash
        br.ReadBytes(256);      // signature
        br.ReadUInt32();        // dataLength (we don't need it)

        // Deflate stream
        using var deflate = new DeflateStream(fs, CompressionMode.Decompress, leaveOpen: true);
        using var dr = new BinaryReader(deflate);

        dr.ReadString(); // modName
        dr.ReadString(); // modVersion

        int fileCount = dr.ReadInt32();
        var entries = new List<TmodEntry>(fileCount);

        for (int i = 0; i < fileCount; i++)
        {
            string name = dr.ReadString();
            int uncompressed = dr.ReadInt32();
            int compressed = dr.ReadInt32();
            entries.Add(new TmodEntry(name, uncompressed, compressed));
        }

        return entries;
    }

    private static void AccumulateFolders(Dictionary<string, long> dict, string folder, long size)
    {
        if (!dict.TryAdd("", size)) dict[""] += size;
        if (string.IsNullOrEmpty(folder)) return;

        string cur = folder;
        while (true)
        {
            if (!dict.TryAdd(cur, size)) dict[cur] += size;
            int slash = cur.LastIndexOf('/');
            if (slash < 0) break;
            cur = cur[..slash];
        }
    }

    private static string GetFolder(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path[..slash] : "";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024d && unit < units.Length - 1) { size /= 1024d; unit++; }
        return $"{size:0.##} {units[unit]}";
    }

    private sealed record TmodEntry(string Name, long UncompressedSize, long CompressedSize);
}
#endif
