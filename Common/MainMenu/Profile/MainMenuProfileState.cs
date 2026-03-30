using PvPAdventure.Common.MainMenu.Achievements;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Common.Skins;
using System;
using System.Collections.Generic;

namespace PvPAdventure.Common.MainMenu.Profile;

internal sealed class MainMenuProfileState
{
    public static MainMenuProfileState Instance { get; } = new();

    private readonly Dictionary<string, int> achievementProgress = [];
    private readonly HashSet<string> collectedAchievements = [];
    private readonly HashSet<SkinIdentity> ownedSkins = [];
    private readonly Dictionary<int, SkinIdentity> equippedSkinsByItemType = [];

    public int Gems { get; private set; }
    public bool HasSyncedFromBackend { get; private set; }

    private MainMenuProfileState()
    {
    }

    public void Reset()
    {
        Gems = 0;
        HasSyncedFromBackend = false;
        achievementProgress.Clear();
        collectedAchievements.Clear();
        ownedSkins.Clear();
        equippedSkinsByItemType.Clear();
    }

    public void SetGems(int gems)
    {
        Gems = Math.Max(0, gems);
    }

    public void SetAchievement(string id, int progress, bool collected)
    {
        achievementProgress[id] = Math.Max(0, progress);

        if (collected)
            collectedAchievements.Add(id);
        else
            collectedAchievements.Remove(id);
    }

    public int GetAchievementProgress(string id)
    {
        return achievementProgress.TryGetValue(id, out int progress) ? progress : 0;
    }

    public bool IsAchievementCollected(string id)
    {
        return collectedAchievements.Contains(id);
    }

    public bool TryCollectAchievement(string id, AchievementDefinition def)
    {
        if (collectedAchievements.Contains(id))
            return false;

        int target = Math.Max(def.Target, 1);
        int progress = Math.Clamp(GetAchievementProgress(id), 0, target);
        if (progress < target)
            return false;

        collectedAchievements.Add(id);
        Gems += def.GemsReward;
        return true;
    }

    public void RevertPurchase(ProductDefinition def)
    {
        Gems += def.Price;
        ownedSkins.Remove(def.Identity);
        equippedSkinsByItemType.Remove(def.ItemType);
    }

    public void SetOwnedSkins(IEnumerable<SkinIdentity> identities)
    {
        ownedSkins.Clear();

        foreach (SkinIdentity identity in identities)
            ownedSkins.Add(identity);
    }

    public void SetEquippedSkin(int itemType, SkinIdentity identity)
    {
        if (!identity.IsValid)
        {
            equippedSkinsByItemType.Remove(itemType);
            return;
        }

        equippedSkinsByItemType[itemType] = identity;
    }

    public bool HasSkin(SkinIdentity identity)
    {
        return ownedSkins.Contains(identity);
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
        return equippedSkinsByItemType.TryGetValue(def.ItemType, out SkinIdentity selectedIdentity) && selectedIdentity == def.Identity;
    }

    public bool TryGetSelectedSkinForItem(int itemType, out SkinIdentity identity)
    {
        return equippedSkinsByItemType.TryGetValue(itemType, out identity);
    }

    public SkinToggleResult ToggleSkin(ProductDefinition def)
    {
        if (!HasSkin(def))
        {
            if (!CanAfford(def))
                return SkinToggleResult.NotAffordable;

            Gems -= def.Price;
            ownedSkins.Add(def.Identity);
            equippedSkinsByItemType[def.ItemType] = def.Identity;
            return SkinToggleResult.Bought;
        }

        if (IsEquipped(def))
        {
            equippedSkinsByItemType.Remove(def.ItemType);
            return SkinToggleResult.Unequipped;
        }

        equippedSkinsByItemType[def.ItemType] = def.Identity;
        return SkinToggleResult.Equipped;
    }

    public void SyncWithBackend(ApiProfileResponse profile, IReadOnlyCollection<ApiInventoryItem> inventory)
    {
        if (profile is null)
        {
            Reset();
            return;
        }

        Gems = Math.Max(0, profile.Gems);
        ownedSkins.Clear();
        equippedSkinsByItemType.Clear();

        Log.Info($"[ProfileState] Sync start. Gems={Gems}, InventoryCount={inventory.Count}, EquipmentCount={profile.Equipment.Count}");

        foreach (ApiInventoryItem item in inventory)
        {
            if (string.IsNullOrWhiteSpace(item.Prototype) || string.IsNullOrWhiteSpace(item.Name))
            {
                Log.Warn("[ProfileState] Skipped invalid inventory item.");
                continue;
            }

            ownedSkins.Add(new SkinIdentity(item.Prototype, item.Name));
        }

        foreach ((string prototype, string name) in profile.Equipment)
        {
            if (!ProductCatalog.TryGet(prototype, name, out ProductDefinition definition))
            {
                Log.Warn($"[ProfileState] Equipped skin missing from ProductCatalog: {prototype}:{name}");
                continue;
            }

            equippedSkinsByItemType[definition.ItemType] = definition.Identity;
        }

        HasSyncedFromBackend = true;
        Log.Info($"[ProfileState] Sync complete. Owned={ownedSkins.Count}, Equipped={equippedSkinsByItemType.Count}");
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