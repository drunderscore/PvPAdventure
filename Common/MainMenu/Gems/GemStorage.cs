using System;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.Gems;

// Keep track of the player's gem count, which is used as currency for the shop.
internal static class GemStorage
{
    private const string KeyGemCount = "GemCount";

    public static int GemCount { get; private set; }

    private static string FilePath => GetFilePath();

    private static string FolderPath
    {
        get
        {
            string? dir = Path.GetDirectoryName(FilePath);
            return string.IsNullOrEmpty(dir) ? Main.SavePath : dir;
        }
    }

    private static string GetFilePath()
    {
        return Path.Combine(Main.SavePath, "PvPAdventure", "Gems.nbt");
    }

    public static int Read()
    {
        if (Main.dedServ)
            return 0;

        try
        {
            if (!File.Exists(FilePath))
            {
                GemCount = 0;
                return GemCount;
            }

            TagCompound tag = TagIO.FromFile(FilePath);
            GemCount = tag.ContainsKey(KeyGemCount) ? tag.GetInt(KeyGemCount) : 0;

            if (GemCount < 0)
                GemCount = 0;

            return GemCount;
        }
        catch (Exception e)
        {
            GemCount = 0;
            Log.Error($"Failed to read Gems.nbt: {e}");
            return GemCount;
        }
    }

    public static void Write(int gemCount)
    {
        if (Main.dedServ)
            return;

        if (gemCount < 0)
            gemCount = 0;

        GemCount = gemCount;

        try
        {
            Directory.CreateDirectory(FolderPath);

            TagCompound tag = new()
            {
                [KeyGemCount] = GemCount
            };

            TagIO.ToFile(tag, FilePath);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to write Gems.nbt: {e}");
        }
    }

    public static void Add(int delta)
    {
        if (Main.dedServ)
            return;

        if (delta == 0)
            return;

        int before = Read();
        int after = before + delta;

        if (after < 0)
            after = 0;

        Log.Info($"Updating gems from {before} to {after}");

        Write(after);
    }

    public static bool TrySpend(int amount)
    {
        if (Main.dedServ)
            return false;

        if (amount <= 0)
            return true;

        int before = Read();
        if (before < amount)
            return false;

        int after = before - amount;

        Log.Info($"Updating gems from {before} to {after}");

        Write(after);
        return true;
    }
}