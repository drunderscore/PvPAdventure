using PvPAdventure.Common.MainMenu.Shop;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinItemNameHooks : ModSystem
{
    public override void Load()
    {
        On_Item.AffixName += OverrideAffixName;
    }
    public override void Unload()
    {
        On_Item.AffixName -= OverrideAffixName;
    }

    private static string OverrideAffixName(On_Item.orig_AffixName orig, Item self)
    {
        string name = orig(self);

        if (self == null || self.IsAir)
            return name;

        string vanillaName = Lang.GetItemNameValue(self.type);
        string replacement = GetDisplayName(self);

        if (replacement == vanillaName)
            return name;

        return name.Replace(vanillaName, replacement);
    }

    public static string GetDisplayName(Item item)
    {
        if (!SkinRegistry.TryGetSkin(item, out ShopProduct skin))
            return Lang.GetItemNameValue(item.type);

        return GetDisplayName(skin, item.type);
    }

    public static string GetDisplayName(ShopProduct skin, int fallbackItemType)
    {
        if (!string.IsNullOrWhiteSpace(skin.DisplayName))
            return skin.DisplayName;

        return Lang.GetItemNameValue(fallbackItemType);
    }
}