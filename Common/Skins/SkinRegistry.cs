using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Shop;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace PvPAdventure.Common.Skins;

internal static class SkinRegistry
{
    private static readonly Dictionary<SkinIdentity, ProductDefinition> ByIdentity;
    private static readonly HashSet<int> SkinnableItemTypes;

    static SkinRegistry()
    {
        ByIdentity = [];
        SkinnableItemTypes = [];

        foreach (ProductDefinition definition in ProductCatalog.All)
        {
            if (!ByIdentity.TryAdd(definition.Identity, definition))
                Log.Error($"Duplicate skin identity '{definition.Prototype}:{definition.Name}' in ProductCatalog.");

            SkinnableItemTypes.Add(definition.ItemType);
        }
    }

    public static bool TryGetByIdentity(SkinIdentity identity, out ProductDefinition definition)
    {
        return ByIdentity.TryGetValue(identity, out definition);
    }

    public static bool TryGetByIdentity(string prototype, string name, out ProductDefinition definition)
    {
        return TryGetByIdentity(new SkinIdentity(prototype, name), out definition);
    }

    public static bool IsSkinnableItemType(int itemType)
    {
        return SkinnableItemTypes.Contains(itemType);
    }

    public static bool TryGetSkin(Item item, out ProductDefinition definition)
    {
        definition = default;

        if (!item.TryGetGlobalItem(out SkinItemData data) || !data.Identity.IsValid)
            return false;

        if (!TryGetByIdentity(data.Identity, out definition))
        {
            Log.Error($"[SkinRegistry] Unknown Skin Identity '{data.Identity.Prototype}:{data.Identity.Name}' on item type={item.type}");
            return false;
        }

        return true;
    }

    public static Texture2D ResolveTexture(ProductDefinition skin, Texture2D vanilla, out bool usingFallback)
    {
        usingFallback = false;
        Asset<Texture2D> asset = skin.Texture;

        if (asset is null || !asset.IsLoaded)
        {
            usingFallback = true;

            if (asset is not null)
                Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            return TextureAssets.Item[ModContent.ItemType<UnloadedItem>()].Value;
        }

        return asset.Value;
    }
}