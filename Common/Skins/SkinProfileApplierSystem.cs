using PvPAdventure.Common.MainMenu.Profile;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinProfileApplierSystem : ModSystem
{
    private static ulong _nextScan;

    public override void PostUpdatePlayers()
    {
        if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            return;

        Player p = Main.LocalPlayer;
        if (p is null || !p.active || Main.GameUpdateCount < _nextScan)
            return;

        _nextScan = Main.GameUpdateCount + 10;
        ProfileStorage.EnsureLoaded();

        foreach (Item item in p.inventory)
        {
            if (item is null || item.IsAir || !SkinRegistry.IsSkinnableItemType(item.type))
                continue;
            if (!item.TryGetGlobalItem(out SkinItemData data))
                continue;

            string newId = ProfileStorage.TryGetSelectedSkinForItem(item.type, out SkinDefinition skin) ? skin.Id : "";
            if (data.SkinId == newId)
                continue;

            data.SkinId = newId;
            item.NetStateChanged();
        }
    }
}