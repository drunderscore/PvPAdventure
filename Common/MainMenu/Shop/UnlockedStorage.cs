using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.MainMenu.Shop;

// Stores unlocked shop items bought from the shop.
internal static class UnlockedStorage
{
    private const string KeyUnlocked = "Unlocked";

    private static bool loaded;
    private static HashSet<string> unlocked = [];

    private static string FilePath => Path.Combine(Main.SavePath, "PvPAdventure", "Unlocked.nbt");

    public static void Load()
    {
        if (Main.dedServ)
            return;

        loaded = true;
        unlocked = [];

        try
        {
            if (!File.Exists(FilePath))
                return;

            TagCompound tag = TagIO.FromFile(FilePath);
            if (!tag.ContainsKey(KeyUnlocked))
                return;

            var list = tag.GetList<string>(KeyUnlocked);
            for (int i = 0; i < list.Count; i++)
                unlocked.Add(list[i]);

            Sanitize();
        }
        catch (Exception e)
        {
            unlocked = [];
            Log.Error($"Failed to load Unlocked.nbt: {e}");
        }
    }

    public static void Save()
    {
        if (Main.dedServ)
            return;

        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            TagCompound tag = new()
            {
                [KeyUnlocked] = new List<string>(unlocked)
            };

            TagIO.ToFile(tag, FilePath);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save Unlocked.nbt: {e}");
        }
    }

    public static bool IsUnlocked(string id)
    {
        EnsureLoaded();
        return unlocked.Contains(id);
    }

    public static bool TryUnlock(string id)
    {
        EnsureLoaded();

        if (!unlocked.Add(id))
            return false;

        Save();
        return true;
    }

    private static void EnsureLoaded()
    {
        if (!loaded)
            Load();
    }

    private static void Sanitize()
    {
        HashSet<string> allowed = [];
        for (int i = 0; i < ShopItems.All.Length; i++)
            allowed.Add(ShopItems.All[i].Id);

        unlocked.RemoveWhere(x => !allowed.Contains(x));
    }
}