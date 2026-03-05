using Terraria;
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

        if (self is null || self.IsAir)
            return name;

        if (!SkinRegistry.TryGetSkin(self, out SkinDefinition skin))
            return name;

        string vanillaName = Lang.GetItemNameValue(self.type);
        string skinnedName = $"{skin.Name} ({vanillaName})";

        if (name.Contains(skinnedName))
            return name;

        return name.Replace(vanillaName, skinnedName);
    }
}