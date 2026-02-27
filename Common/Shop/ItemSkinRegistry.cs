using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.Shop;

internal static class ItemSkinRegistry
{
    internal readonly record struct Skin(string UnlockId, string Name, Func<Texture2D> Texture);

    private static readonly Dictionary<int, Skin[]> SkinsByItem = new()
    {
        [ItemID.SniperRifle] =
        [
            new Skin("red_sniper_rifle", "Red Sniper Rifle", () => Ass.RedSniperRifle.Value),
            new Skin("pink_sniper_rifle", "Pink Sniper Rifle", () => Ass.PinkSniperRifle.Value),
        ],

        // Add more:
        // [ItemID.Xenopopper] = [ new Skin("blue_xenopopper", "Blue Xenopopper", () => Ass.BlueXenopopper.Value) ],
    };

    public static bool HasAnySkins(int itemType) => SkinsByItem.ContainsKey(itemType);

    public static bool TryGetSkin(Item item, out Texture2D tex, out string name)
    {
        tex = null!;
        name = string.Empty;

        if (!SkinsByItem.TryGetValue(item.type, out Skin[] skins))
            return false;

        for (int i = 0; i < skins.Length; i++)
        {
            Skin s = skins[i];
            if (!UnlockedStorage.IsUnlocked(s.UnlockId))
                continue;

            tex = s.Texture();
            name = s.Name;
            return true;
        }

        return false;
    }
}