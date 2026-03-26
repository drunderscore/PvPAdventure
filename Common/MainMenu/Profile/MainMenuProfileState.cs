using System;
using System.Collections.Generic;
using PvPAdventure.Common.MainMenu.Achievements;
using PvPAdventure.Common.Skins;

namespace PvPAdventure.Common.MainMenu.Profile;

internal sealed class MainMenuProfileState
{
    public static MainMenuProfileState Instance { get; } = new();

    private readonly Dictionary<string, int> _achievementProgress = [];
    private readonly HashSet<string> _collectedAchievements = [];
    private readonly HashSet<string> _ownedSkins = [];
    private readonly Dictionary<int, string> _equippedSkinsByItemType = [];

    public int Gems { get; private set; }

    private MainMenuProfileState()
    {
    }

    public void Reset()
    {
        Gems = 0;
        _achievementProgress.Clear();
        _collectedAchievements.Clear();
        _ownedSkins.Clear();
        _equippedSkinsByItemType.Clear();
    }

    public void SetGems(int gems)
    {
        Gems = Math.Max(0, gems);
    }

    public void SetAchievement(string id, int progress, bool collected)
    {
        _achievementProgress[id] = Math.Max(0, progress);

        if (collected)
            _collectedAchievements.Add(id);
        else
            _collectedAchievements.Remove(id);
    }

    public int GetAchievementProgress(string id)
    {
        return _achievementProgress.TryGetValue(id, out int progress) ? progress : 0;
    }

    public bool IsAchievementCollected(string id)
    {
        return _collectedAchievements.Contains(id);
    }

    public bool TryCollectAchievement(string id, AchievementDefinition def)
    {
        if (_collectedAchievements.Contains(id))
            return false;

        int target = Math.Max(def.Target, 1);
        int progress = Math.Clamp(GetAchievementProgress(id), 0, target);
        if (progress < target)
            return false;

        _collectedAchievements.Add(id);
        Gems += def.GemsReward;
        return true;
    }

    public void SetOwnedSkins(IEnumerable<string> skinIds)
    {
        _ownedSkins.Clear();

        foreach (string skinId in skinIds)
            _ownedSkins.Add(skinId);
    }

    public void SetEquippedSkin(int itemType, string? skinId)
    {
        if (string.IsNullOrEmpty(skinId))
        {
            _equippedSkinsByItemType.Remove(itemType);
            return;
        }

        _equippedSkinsByItemType[itemType] = skinId;
    }

    public bool HasSkin(string skinId)
    {
        return _ownedSkins.Contains(skinId);
    }

    public bool HasSkin(SkinDefinition def)
    {
        return HasSkin(def.Id);
    }

    public bool CanAfford(SkinDefinition def)
    {
        return Gems >= def.Price;
    }

    public bool IsEquipped(SkinDefinition def)
    {
        return _equippedSkinsByItemType.TryGetValue(def.ItemType, out string selectedSkinId) && selectedSkinId == def.Id;
    }

    public bool TryGetSelectedSkinForItem(int itemType, out string skinId)
    {
        return _equippedSkinsByItemType.TryGetValue(itemType, out skinId!);
    }

    public SkinToggleResult ToggleSkin(SkinDefinition def)
    {
        if (!HasSkin(def))
        {
            if (!CanAfford(def))
                return SkinToggleResult.NotAffordable;

            Gems -= def.Price;
            _ownedSkins.Add(def.Id);
            _equippedSkinsByItemType[def.ItemType] = def.Id;
            return SkinToggleResult.Bought;
        }

        if (IsEquipped(def))
        {
            _equippedSkinsByItemType.Remove(def.ItemType);
            return SkinToggleResult.Unequipped;
        }

        _equippedSkinsByItemType[def.ItemType] = def.Id;
        return SkinToggleResult.Equipped;
    }
}

internal enum SkinToggleResult
{
    None,
    Bought,
    Equipped,
    Unequipped,
    NotAffordable
}