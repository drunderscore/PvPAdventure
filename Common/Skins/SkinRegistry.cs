using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace PvPAdventure.Common.Skins;

internal static class SkinRegistry
{
    private static readonly Dictionary<string, SkinDefinition> _byId;
    private static readonly HashSet<int> _skinnable;

    static SkinRegistry()
    {
        _byId = new Dictionary<string, SkinDefinition>(SkinCatalog.All.Length);
        _skinnable = new HashSet<int>(SkinCatalog.All.Length);

        foreach (SkinDefinition def in SkinCatalog.All)
        {
            if (!_byId.TryAdd(def.Id, def))
                Log.Error($"Duplicate skin id '{def.Id}' in SkinCatalog.All.");
            _skinnable.Add(def.ItemType);
        }
    }

    public static bool TryGetById(string id, out SkinDefinition def) => _byId.TryGetValue(id, out def);
    public static bool IsSkinnableItemType(int itemType) => _skinnable.Contains(itemType);

    public static bool TryGetSkin(Item item, out SkinDefinition def)
    {
        def = default;
        if (!item.TryGetGlobalItem(out SkinItemData data) || string.IsNullOrEmpty(data.SkinId))
            return false;

        if (!TryGetById(data.SkinId, out def))
        {
            Log.Error($"[SkinRegistry] Unknown SkinId '{data.SkinId}' on item type={item.type}");
            return false;
        }

        return true;
    }

    public static Texture2D ResolveTexture(SkinDefinition skin, Texture2D vanilla, out bool usingFallback)
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