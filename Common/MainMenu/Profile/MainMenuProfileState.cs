using PvPAdventure.Common.MainMenu.API.Profile;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Common.Skins;
using System;
using System.Collections.Generic;

namespace PvPAdventure.Common.MainMenu.Profile;

internal sealed class MainMenuProfileState
{
    public static MainMenuProfileState Instance { get; } = new();

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
        ownedSkins.Clear();
        equippedSkinsByItemType.Clear();
        SkinProfileApplierSystem.RequestApply();
    }

    public bool HasSkin(ProductDefinition def)
    {
        return ownedSkins.Contains(def.Identity);
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
            SkinIdentity identity = new(item.Prototype, item.Name);
            if (identity.IsValid)
                ownedSkins.Add(identity);
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
        SkinProfileApplierSystem.RequestApply();
        Log.Info($"[ProfileState] Sync complete. Gems={Gems}, Owned={ownedSkins.Count}, Equipped={equippedSkinsByItemType.Count}");
    }
}
