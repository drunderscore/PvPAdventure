using PvPAdventure.Common.MainMenu.Achievements;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Common.Skins;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;
using AchievementCatalog = PvPAdventure.Common.MainMenu.Achievements.Achievements;

namespace PvPAdventure.Common.MainMenu.Profile;

internal static class ProfileStorage
{
    private static string Folder => Path.Combine(Main.SavePath, "PvPAdventure");
    private static string File => Path.Combine(Folder, "Profile.nbt");

    private static bool loaded;

    public static int Gems { get; set; }
    public static HashSet<string> Skins { get; } = [];
    public static AchievementProgress Achievements { get; set; } = new();

    public static void EnsureLoaded()
    {
        if (!loaded)
            Load();
    }

    public static void Load()
    {
        if (Main.dedServ)
            return;

        loaded = true;
        Reset();
        TryLoad(File);
    }

    public static void Save()
    {
        if (Main.dedServ)
            return;

        try
        {
            Directory.CreateDirectory(Folder);

            TagCompound root = new()
            {
                ["Gems"] = Gems,
                ["Skins"] = new List<string>(Skins),
                ["Achievements"] = Achievements.ToTag(),
            };

            TagIO.ToFile(root, File);
        }
        catch (Exception e)
        {
            Log.Error($"Profile save failed: {e}");
        }
    }

    public static void AddGems(int amount)
    {
        EnsureLoaded();

        if (amount <= 0)
            return;

        Gems += amount;
        Save();
    }

    public static bool SpendGems(int amount)
    {
        EnsureLoaded();

        if (amount <= 0)
            return true;

        if (Gems < amount)
            return false;

        Gems -= amount;
        Save();
        return true;
    }

    public static void RebuildGems(IReadOnlyList<MatchResult> matches)
    {
        EnsureLoaded();

        int earned = 0;

        foreach (var match in matches)
            earned += MatchStorage.GetPlacementGems(MatchStorage.GetLocalPlacement(match));

        for (int i = 0; i < AchievementCatalog.All.Length; i++)
        {
            var (id, def) = AchievementCatalog.All[i];
            if (Achievements.IsCollected(id))
                earned += def.GemsReward;
        }

        int spent = GetSpentGemsFromSkins();

        Gems = Math.Max(0, earned - spent);
        Save();
    }

    private static int GetSpentGemsFromSkins()
    {
        int spent = 0;
        foreach (string id in Skins)
            if (SkinRegistry.TryGetById(id, out SkinDefinition def))
                spent += def.Price;
        return spent;
    }

    public enum SkinToggleResult : byte
    {
        None,
        Bought,
        Sold,
    }

    public static bool HasSkin(string id)
    {
        EnsureLoaded();
        return Skins.Contains(id);
    }

    public static bool TryGetSelectedSkinForItem(int itemType, out SkinDefinition selected)
    {
        EnsureLoaded();

        for (int i = 0; i < SkinCatalog.All.Length; i++)
        {
            SkinDefinition def = SkinCatalog.All[i];

            if (def.ItemType != itemType)
                continue;

            if (!Skins.Contains(def.Id))
                continue;

            selected = def;
            return true;
        }

        selected = default;
        return false;
    }

    public static SkinToggleResult ToggleSkin(SkinDefinition def)
    {
        EnsureLoaded();

        if (Skins.Contains(def.Id))
        {
            Skins.Remove(def.Id);
            Gems += def.Price;
            Save();
            return SkinToggleResult.Sold;
        }

        // Disallow buying if you already have a skin for this weapon (no swapping).
        if (TryGetSelectedSkinForItem(def.ItemType, out _))
            return SkinToggleResult.None;

        if (Gems < def.Price)
            return SkinToggleResult.None;

        Skins.Add(def.Id);
        Gems -= def.Price;

        Save();
        return SkinToggleResult.Bought;
    }

    public static bool TryCollect(string id)
    {
        EnsureLoaded();

        for (int i = 0; i < AchievementCatalog.All.Length; i++)
        {
            var (aid, def) = AchievementCatalog.All[i];
            if (aid != id)
                continue;

            if (!Achievements.TryCollect(id, def.Target))
                return false;

            AddGems(def.GemsReward);
            Save();
            return true;
        }

        return false;
    }

    public static void ApplyMatch(MatchResult match)
    {
        EnsureLoaded();

        bool changed = false;

        for (int i = 0; i < AchievementCatalog.All.Length; i++)
        {
            var (id, def) = AchievementCatalog.All[i];
            changed |= Achievements.Add(id, def.Target, def.Delta(match));
        }

        if (changed)
            Save();
    }

    public static void RebuildAchievements(IReadOnlyList<MatchResult> matches)
    {
        EnsureLoaded();

        AchievementProgress rebuilt = new()
        {
            Collected = [.. Achievements.Collected]
        };

        Achievements = rebuilt;

        foreach (var match in matches)
        {
            for (int j = 0; j < AchievementCatalog.All.Length; j++)
            {
                var (id, def) = AchievementCatalog.All[j];
                rebuilt.Add(id, def.Target, def.Delta(match));
            }
        }

        rebuilt.Sanitize();
        Save();
    }

    private static void Reset()
    {
        Gems = 0;
        Skins.Clear();
        Achievements = new();
    }

    private static void TryLoad(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return;

            TagCompound root = TagIO.FromFile(path);

            Gems = Math.Max(0, root.ContainsKey("Gems") ? root.GetInt("Gems") : 0);

            if (root.ContainsKey("Skins"))
                foreach (var s in root.GetList<string>("Skins"))
                    Skins.Add(s);

            if (root.ContainsKey("Unlocked"))
                foreach (var s in root.GetList<string>("Unlocked"))
                    Skins.Add(s);

            if (root.ContainsKey("Achievements"))
                Achievements = AchievementProgress.FromTag(root.Get<TagCompound>("Achievements"));

            if (root.ContainsKey("SelectedSkins"))
            {
                foreach (TagCompound entry in root.GetList<TagCompound>("SelectedSkins"))
                {
                    int itemType = entry.GetInt("I");
                    string id = entry.GetString("S");

                    RemoveAllSkinsForItem(itemType);

                    if (SkinRegistry.TryGetById(id, out _))
                        Skins.Add(id);
                }
            }

            Skins.RemoveWhere(x => !SkinRegistry.TryGetById(x, out _));
            SanitizeSkins();
        }
        catch (Exception e)
        {
            Log.Error($"Profile load failed: {e}");
            Reset();
        }
    }

    private static void RemoveAllSkinsForItem(int itemType)
    {
        for (int i = 0; i < SkinCatalog.All.Length; i++)
        {
            SkinDefinition def = SkinCatalog.All[i];

            if (def.ItemType != itemType)
                continue;

            Skins.Remove(def.Id);
        }
    }

    private static void SanitizeSkins()
    {
        Dictionary<int, string> chosen = [];

        for (int i = 0; i < SkinCatalog.All.Length; i++)
        {
            SkinDefinition def = SkinCatalog.All[i];

            if (!Skins.Contains(def.Id))
                continue;

            chosen[def.ItemType] = def.Id;
        }

        Skins.Clear();

        foreach (var id in chosen.Values)
            Skins.Add(id);
    }
}