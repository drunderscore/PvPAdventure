using System;
using System.Collections.Generic;
using PvPAdventure.Common.MainMenu.Achievements;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Common.Skins;

namespace PvPAdventure.Common.MainMenu.Profile;

internal sealed class MainMenuProfileState
{
    public static MainMenuProfileState Instance { get; } = new();

    private readonly Dictionary<string, int> _achievementProgress = [];
    private readonly HashSet<string> _collectedAchievements = [];

    private readonly HashSet<SkinIdentity> _ownedSkins = [];
    private readonly Dictionary<int, SkinIdentity> _equippedSkinsByItemType = [];

    public int Gems { get; private set; }
    public bool HasSyncedFromBackend { get; private set; }

    private MainMenuProfileState()
    {
    }

    public void Reset()
    {
        Gems = 0;
        HasSyncedFromBackend = false;
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

    public void RevertPurchase(ProductDefinition def)
    {
        Gems += def.Price;
        _ownedSkins.Remove(def.Identity);
        _equippedSkinsByItemType.Remove(def.ItemType);
    }

    public void SetOwnedSkins(IEnumerable<SkinIdentity> identities)
    {
        _ownedSkins.Clear();

        foreach (SkinIdentity identity in identities)
            _ownedSkins.Add(identity);
    }

    public void SetEquippedSkin(int itemType, SkinIdentity identity)
    {
        if (!identity.IsValid)
        {
            _equippedSkinsByItemType.Remove(itemType);
            return;
        }

        _equippedSkinsByItemType[itemType] = identity;
    }

    public bool HasSkin(SkinIdentity identity)
    {
        return _ownedSkins.Contains(identity);
    }

    public bool HasSkin(ProductDefinition def)
    {
        return HasSkin(def.Identity);
    }

    public bool CanAfford(ProductDefinition def)
    {
        return Gems >= def.Price;
    }

    public bool IsEquipped(ProductDefinition def)
    {
        return _equippedSkinsByItemType.TryGetValue(def.ItemType, out SkinIdentity selectedIdentity) && selectedIdentity == def.Identity;
    }

    public bool TryGetSelectedSkinForItem(int itemType, out SkinIdentity identity)
    {
        return _equippedSkinsByItemType.TryGetValue(itemType, out identity);
    }

    public SkinToggleResult ToggleSkin(ProductDefinition def)
    {
        if (!HasSkin(def))
        {
            if (!CanAfford(def))
                return SkinToggleResult.NotAffordable;

            Gems -= def.Price;
            _ownedSkins.Add(def.Identity);
            _equippedSkinsByItemType[def.ItemType] = def.Identity;
            return SkinToggleResult.Bought;
        }

        if (IsEquipped(def))
        {
            _equippedSkinsByItemType.Remove(def.ItemType);
            return SkinToggleResult.Unequipped;
        }

        _equippedSkinsByItemType[def.ItemType] = def.Identity;
        return SkinToggleResult.Equipped;
    }

    public void SyncWithBackend(ApiProfileResponse profile, List<ApiInventoryItem> inventory)
    {
        Gems = profile.Gems;
        _ownedSkins.Clear();
        _equippedSkinsByItemType.Clear();

        Log.Info($"[ProfileState] Sync start. Gems={profile.Gems}, inventoryCount={inventory?.Count ?? 0}, equipmentCount={profile.Equipment?.Count ?? 0}");

        if (inventory != null)
        {
            foreach (ApiInventoryItem item in inventory)
            {
                SkinIdentity identity = new(item.Prototype, item.Name);
                _ownedSkins.Add(identity);
                Log.Info($"[ProfileState] Owned skin loaded: {item.Prototype}:{item.Name}");
            }
        }

        if (profile.Equipment != null)
        {
            foreach ((string prototype, string name) in profile.Equipment)
            {
                if (ProductCatalog.TryGet(prototype, name, out ProductDefinition def))
                {
                    _equippedSkinsByItemType[def.ItemType] = def.Identity;
                    Log.Info($"[ProfileState] Equipped skin loaded: {prototype}:{name} -> itemType={def.ItemType}");
                }
                else
                {
                    Log.Error($"[ProfileState] Equipped skin missing from ProductCatalog: {prototype}:{name}");
                }
            }
        }

        HasSyncedFromBackend = true;
        Log.Info($"[ProfileState] Sync complete. Owned={_ownedSkins.Count}, Equipped={_equippedSkinsByItemType.Count}");
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