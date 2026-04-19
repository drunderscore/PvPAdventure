//using PvPAdventure.Common.MainMenu.Shop;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

//[Autoload(Side = ModSide.Client)]
//internal sealed class SkinProfileApplierSystem : ModSystem
//{
//    private bool _pendingApply;
//    private int _applyDelay;
//    private int _applyAttempts;

//    public override void OnWorldLoad()
//    {
//        _pendingApply = true;
//        _applyDelay = 15;
//        _applyAttempts = 0;

//        Log.Info("[SkinProfileApplier] World loaded. Waiting to apply equipped skins.");
//    }

//    public override void PostUpdatePlayers()
//    {
//        LogEquippedSkinsEverySeconds(5);

//        if (!_pendingApply)
//            return;

//        if (_applyDelay > 0)
//        {
//            _applyDelay--;
//            return;
//        }

//        _applyDelay = 15;
//        _applyAttempts++;

//        if (TryApplyEquippedSkinsToInventory())
//        {
//            _pendingApply = false;
//            Log.Info($"[SkinProfileApplier] Finished applying skins after {_applyAttempts} attempt(s).");
//            return;
//        }

//        if (_applyAttempts >= 20)
//        {
//            _pendingApply = false;
//            Log.Error("[SkinProfileApplier] Gave up applying skins after 20 attempts.");
//        }
//    }

//    private static bool TryApplyEquippedSkinsToInventory()
//    {
//        Player player = Main.LocalPlayer;
//        MainMenuProfileState state = MainMenuProfileState.Instance;

//        if (player is null || !player.active)
//        {
//            Log.Info("[SkinProfileApplier] LocalPlayer not ready yet.");
//            return false;
//        }

//        if (!state.HasSyncedFromBackend)
//        {
//            Log.Info("[SkinProfileApplier] Waiting for backend profile sync.");
//            return false;
//        }

//        Log.Info("[SkinProfileApplier] Backend sync is ready. Applying skins to inventory.");

//        bool inventoryChanged = false;

//        for (int i = 0; i < 59; i++)
//        {
//            Item item = player.inventory[i];
//            if (item is null || item.IsAir || !SkinRegistry.IsSkinnableItemType(item.type))
//                continue;

//            if (!item.TryGetGlobalItem(out SkinItemData data))
//            {
//                Log.Error($"[SkinProfileApplier] Slot {i}: failed to get SkinItemData for item type {item.type}.");
//                continue;
//            }

//            bool hasEquippedSkin = state.TryGetSelectedSkinForItem(item.type, out SkinIdentity equippedIdentity);

//            Log.Info($"[SkinProfileApplier] Slot {i}: itemType={item.type}, itemName={item.Name}, currentSkin={data.Identity.Prototype}:{data.Identity.Name}, hasEquipped={hasEquippedSkin}, equippedSkin={equippedIdentity.Prototype}:{equippedIdentity.Name}");

//            if (data.Identity == equippedIdentity)
//                continue;

//            data.Identity = equippedIdentity;
//            inventoryChanged = true;

//            Log.Info($"[SkinProfileApplier] Applied skin to slot {i}: {equippedIdentity.Prototype}:{equippedIdentity.Name}");

//            if (Main.netMode == NetmodeID.MultiplayerClient)
//                SkinNetHandler.SendSelectedSkin(item.type, equippedIdentity);
//        }

//        if (inventoryChanged)
//        {
//            Log.Info("[SkinProfileApplier] Inventory skins changed. Syncing player.");

//            if (Main.netMode == NetmodeID.MultiplayerClient)
//                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
//        }
//        else
//        {
//            Log.Info("[SkinProfileApplier] No inventory skin changes were needed.");
//        }

//        return true;
//    }

//    private static void LogEquippedSkinsEverySeconds(int seconds)
//    {
//        if (seconds <= 0 || Main.GameUpdateCount % (60 * seconds) != 0)
//            return;

//        MainMenuProfileState state = MainMenuProfileState.Instance;

//        Log.Info("[SkinProfileApplier] Equipped skins snapshot:");

//        foreach (ProductDefinition definition in ProductCatalog.All)
//        {
//            if (!state.TryGetSelectedSkinForItem(definition.ItemType, out SkinIdentity identity) || !identity.IsValid)
//                continue;

//            Log.Info($"[SkinProfileApplier] itemType={definition.ItemType}, equipped={identity.Prototype}:{identity.Name}");
//        }
//    }
//}