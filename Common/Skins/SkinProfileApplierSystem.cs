using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Common.MainMenu.Shop;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinProfileApplierSystem : ModSystem
{
    private bool pendingApply;

    public static void RequestApply()
    {
        if (Main.dedServ)
            return;

        ModContent.GetInstance<SkinProfileApplierSystem>().pendingApply = true;
    }

    public override void OnWorldLoad()
    {
        pendingApply = true;
    }

    public override void PostUpdatePlayers()
    {
        if (!pendingApply && Main.GameUpdateCount % 60 != 0)
            return;

        pendingApply = false;
        TryApplyEquippedSkinsToInventory();
    }

    private static void TryApplyEquippedSkinsToInventory()
    {
        Player player = Main.LocalPlayer;
        MainMenuProfileState state = MainMenuProfileState.Instance;

        if (player == null || !player.active || !state.HasSyncedFromBackend)
            return;

        bool changed = false;
        HashSet<int> syncedItemTypes = [];

        foreach (Item item in player.inventory)
        {
            if (item == null || item.IsAir || !SkinRegistry.IsSkinnableItemType(item.type))
                continue;

            if (!item.TryGetGlobalItem(out SkinItemData data))
                continue;

            state.TryGetSelectedSkinForItem(item.type, out ProductKey key);
            if (data.Key == key)
                continue;

            data.Key = key;
            changed = true;

            if (Main.netMode == NetmodeID.MultiplayerClient && syncedItemTypes.Add(item.type))
                SkinNetHandler.SendSelectedSkin(item.type, key, force: true);
        }

        if (changed && Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI);
    }
}