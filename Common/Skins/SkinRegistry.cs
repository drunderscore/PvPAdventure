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
    private static readonly Dictionary<SkinIdentity, ProductDefinition> _byIdentity;
    private static readonly HashSet<int> _skinnable;

    static SkinRegistry()
    {
        _byIdentity = new Dictionary<SkinIdentity, ProductDefinition>(Products.All.Length);
        _skinnable = new HashSet<int>(Products.All.Length);

        foreach (ProductDefinition def in Products.All)
        {
            if (!_byIdentity.TryAdd(def.Identity, def))
                Log.Error($"Duplicate skin identity '{def.Prototype}:{def.Name}' in Products.All.");

            _skinnable.Add(def.ItemType);
        }
    }

    public static bool TryGetByIdentity(SkinIdentity identity, out ProductDefinition def) => _byIdentity.TryGetValue(identity, out def);
    public static bool IsSkinnableItemType(int itemType) => _skinnable.Contains(itemType);

    public static bool TryGetSkin(Item item, out ProductDefinition def)
    {
        def = default;
        if (!item.TryGetGlobalItem(out SkinItemData data) || !data.Identity.IsValid)
            return false;

        if (!TryGetByIdentity(data.Identity, out def))
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