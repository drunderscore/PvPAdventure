using PvPAdventure.Common.MainMenu.API.Profile;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Common.Skins;
using System;
using System.Collections.Generic;

namespace PvPAdventure.Common.MainMenu.Profile;

internal sealed class MainMenuProfileState
{
    public static MainMenuProfileState Instance { get; } = new();

    private readonly HashSet<ProductKey> ownedSkins = [];
    private readonly Dictionary<int, ProductKey> equippedSkinsByItemType = [];

    public int Gems { get; private set; }
    public bool HasSyncedFromBackend { get; private set; }

    private MainMenuProfileState()
    {
    }

    public void Reset()
    {
        Gems = 0;
        HasSyncedFromBackend = false;
        ownedSkins.Clear();
        equippedSkinsByItemType.Clear();
        SkinProfileApplierSystem.RequestApply();
    }

    public bool HasSkin(ShopProduct def)
    {
        return ownedSkins.Contains(new ProductKey(def.Prototype, def.Name));
    }

    public bool CanAfford(ShopProduct def)
    {
        return Gems >= def.Price;
    }

    public bool IsEquipped(ShopProduct def)
    {
        return equippedSkinsByItemType.TryGetValue(def.ItemType, out ProductKey key) && key == new ProductKey(def.Prototype, def.Name);
    }

    public bool TryGetSelectedSkinForItem(int itemType, out ProductKey key)
    {
        return equippedSkinsByItemType.TryGetValue(itemType, out key);
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

        foreach (ApiInventoryItem item in inventory)
        {
            ProductKey key = new(item.Prototype, item.Name);
            if (key.IsValid)
                ownedSkins.Add(key);
        }

        foreach ((string prototype, string name) in profile.Equipment)
        {
            if (!ProductCatalog.TryGet(prototype, name, out ShopProduct definition))
            {
                Log.Warn($"[ProfileState] Equipped skin missing from ProductCatalog: {prototype}:{name}");
                continue;
            }

            equippedSkinsByItemType[definition.ItemType] = new ProductKey(definition.Prototype, definition.Name);
        }

        HasSyncedFromBackend = true;
        SkinProfileApplierSystem.RequestApply();
        Log.Info($"[ProfileState] Sync complete. Gems={Gems}, Owned={ownedSkins.Count}, Equipped={equippedSkinsByItemType.Count}");
    }
}