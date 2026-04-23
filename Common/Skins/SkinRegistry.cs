using PvPAdventure.Common.MainMenu.Shop;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Microsoft.Xna.Framework.Graphics;

namespace PvPAdventure.Common.Skins;

internal static class SkinRegistry
{
    private static readonly Dictionary<ProductKey, ShopProduct> ByKey;
    private static readonly HashSet<int> SkinnableItemTypes;

    static SkinRegistry()
    {
        ByKey = [];
        SkinnableItemTypes = [];

        foreach (ShopProduct definition in ProductCatalog.All)
        {
            ProductKey key = new(definition.Prototype, definition.Name);

            if (!ByKey.TryAdd(key, definition))
                Log.Error($"Duplicate skin identity '{definition.Prototype}:{definition.Name}' in ProductCatalog.");

            SkinnableItemTypes.Add(definition.ItemType);
        }
    }

    public static bool TryGetByKey(ProductKey key, out ShopProduct definition)
    {
        return ByKey.TryGetValue(key, out definition);
    }

    public static bool TryGetByKey(string prototype, string name, out ShopProduct definition)
    {
        return TryGetByKey(new ProductKey(prototype, name), out definition);
    }

    public static bool IsSkinnableItemType(int itemType)
    {
        return SkinnableItemTypes.Contains(itemType);
    }

    public static bool TryGetSkin(Item item, out ShopProduct definition)
    {
        definition = default;

        if (!item.TryGetGlobalItem(out SkinItemData data) || !data.Key.IsValid)
            return false;

        if (!TryGetByKey(data.Key, out definition))
        {
            Log.Error($"[SkinRegistry] Unknown ProductKey '{data.Key.Prototype}:{data.Key.Name}' on item type={item.type}");
            return false;
        }

        return true;
    }

    public static Texture2D ResolveTexture(ShopProduct skin, Texture2D vanilla, out bool usingFallback)
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