using PvPAdventure.Content.Items;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector.Systems;

/// <summary>
/// Prevents the Adventure Mirror from being removed from the player inventory via trashing, selling etc.
/// Various inventory hooks are used to achieve this.
/// </summary>
public class ItemSlotHooks : ModSystem
{
    public override void Load()
    {
        On_ItemSlot.LeftClick_SellOrTrash += Hook_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int += Hook_LeftClick_ItemArray;
        On_ItemSlot.RightClick_ItemArray_int_int += Hook_RightClick;
        On_Player.SellItem += Hook_SellItem;
        On_Player.DropSelectedItem += Hook_DropSelectedItem;
        On_Player.dropItemCheck += Hook_DropItemCheck;
    }

    public override void Unload()
    {
        On_ItemSlot.LeftClick_SellOrTrash -= Hook_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int -= Hook_LeftClick_ItemArray;
        On_Player.SellItem -= Hook_SellItem;
        On_Player.DropSelectedItem -= Hook_DropSelectedItem;
        On_Player.dropItemCheck -= Hook_DropItemCheck;
    }

    // Vanilla method with the item animation check removed for Adventure Mirror.
    private static void Hook_RightClick(
        On_ItemSlot.orig_RightClick_ItemArray_int_int orig,
        Item[] inv, int context, int slot)
    {
        Player player = Main.player[Main.myPlayer];
        inv[slot].newAndShiny = false;
        if (player.itemAnimation > 0 && player.HeldItem.type != ModContent.ItemType<AdventureMirror>())
        {
            return;
        }
        if (context == 15)
        {
            ItemSlot.HandleShopSlot(inv, slot, rightClickIsValid: true, leftClickIsValid: false);
        }
        else
        {
            if (!Main.mouseRight)
            {
                return;
            }
            if (context == 0 && Main.mouseRightRelease)
            {
                ItemSlot.TryItemSwap(inv[slot]);
            }
            if (context == 0 && ItemLoader.CanRightClick(inv[slot]))
            {
                if (Main.mouseRightRelease)
                {
                    if (Main.ItemDropsDB.GetRulesForItemID(inv[slot].type).Any())
                    {
                        ItemSlot.TryOpenContainer(inv[slot], player);
                    }
                    else
                    {
                        ItemLoader.RightClick(inv[slot], player);
                    }
                }
                return;
            }
            switch (Math.Abs(context))
            {
                case 9:
                case 11:
                    if (Main.mouseRightRelease)
                    {
                        ItemSlot.SwapVanityEquip(inv, context, slot, player);
                    }
                    break;
                case 12:
                case 25:
                case 27:
                case 33:
                    if (Main.mouseRightRelease)
                    {
                        ItemSlot.TryPickupDyeToCursor(context, inv, slot, player);
                    }
                    break;
                case 0:
                case 3:
                case 4:
                case 32:
                    if (inv[slot].maxStack == 1)
                    {
                        if (Main.mouseRightRelease)
                        {
                            ItemSlot.SwapEquip(inv, context, slot);
                        }
                        break;
                    }
                    goto default;
                default:
                    {
                        if (Main.stackSplit > 1)
                        {
                            break;
                        }
                        bool flag = true;
                        bool flag2 = inv[slot].maxStack <= 1 && inv[slot].stack <= 1;
                        if (context == 0 && flag2)
                        {
                            flag = false;
                        }
                        if (context == 3 && flag2)
                        {
                            flag = false;
                        }
                        if (context == 4 && flag2)
                        {
                            flag = false;
                        }
                        if (context == 32 && flag2)
                        {
                            flag = false;
                        }
                        if (!flag)
                        {
                            break;
                        }
                        int num = Main.superFastStack + 1;
                        for (int i = 0; i < num; i++)
                        {
                            if (((Main.mouseItem.IsTheSameAs(inv[slot]) && ItemLoader.CanStack(Main.mouseItem, inv[slot])) || Main.mouseItem.type == 0) && (Main.mouseItem.stack < Main.mouseItem.maxStack || Main.mouseItem.type == 0))
                            {
                                ItemSlot.PickupItemIntoMouse(inv, context, slot, player);
                                SoundEngine.PlaySound(12);
                                ItemSlot.RefreshStackSplitCooldown();
                            }
                        }
                        break;
                    }
            }
        }
    }

    private void Hook_DropItemCheck(On_Player.orig_dropItemCheck orig, Player self)
    {
        // If the mouse is holding the Adventure Mirror, just block the drop.
        if (IsAdventureMirror(Main.mouseItem))
        {
            // Note, this makes the item unusable out of the inventory
            //return; 
        }
        orig(self);
    }

    private void Hook_DropSelectedItem(On_Player.orig_DropSelectedItem orig,Player self)
    {
        // If the selected item is the Adventure Mirror, block the drop.
        Item selectedItem = self.inventory[self.selectedItem];
        if (IsAdventureMirror(selectedItem))
        {
            return;
        }
        orig(self);
    }

    // QUICK TRASH / QUICK SELL (Shift + click)
    private static bool Hook_LeftClick_SellOrTrash(
        On_ItemSlot.orig_LeftClick_SellOrTrash orig,
        Item[] inv, int context, int slot)
    {
        Item item = inv[slot];

        if (!item.IsAir && item.type == ModContent.ItemType<AdventureMirror>())
        {
            //if (Main.keyState.IsKeyDown(Keys.LeftShift) && Main.mouseLeft && Main.mouseLeftRelease)
                //PopupTextHelper.NewText("Cannot quick trash Adventure Mirror!");
            
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return false; // skip SellOrTrash completely
        }

        return orig(inv, context, slot);
    }

    // Helper method to identify Adventure Mirror
    private static bool IsAdventureMirror(Item item)
    => !item.IsAir && item.type == ModContent.ItemType<AdventureMirror>();

    // Vanilla method with the item animation check removed for Adventure Mirror.
    // DRAGGING ONTO THE TRASH SLOT / CHESTS / BANKS
    private static void Hook_LeftClick_ItemArray(
        On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,
        Item[] inv, int context, int slot)
    {
        bool isMirrorOnMouse = IsAdventureMirror(Main.mouseItem);

        // 1) Block putting mirror into any storage (chest / banks)
        bool isStorageSlot =
            context == ItemSlot.Context.ChestItem ||
            context == ItemSlot.Context.BankItem; // Piggy Bank

        if (isStorageSlot && isMirrorOnMouse)
        {
            //if (Main.mouseLeft && Main.mouseLeftRelease)
                //Main.NewText("Cannot store Adventure Mirror!");

            // Do NOT call orig -> cancel the placement
            return;
        }

        // 2) Block dragging mirror onto trash slot
        if (context == ItemSlot.Context.TrashItem && isMirrorOnMouse)
        {
            //if (Main.mouseLeft && Main.mouseLeftRelease)
                //PopupTextHelper.NewText("Cannot trash Adventure Mirror!");

            return; // cancel trashing
        }

        // Otherwise, normal behavior
        //orig(inv, context, slot);

        // --- Normal behaviour ---
        Player player = Main.player[Main.myPlayer];
        bool flag = Main.mouseLeftRelease && Main.mouseLeft;
        if (flag)
        {
            if (ItemSlot.OverrideLeftClick(inv, context, slot))
            {
                return;
            }
            inv[slot].newAndShiny = false;
            if (ItemSlot.LeftClick_SellOrTrash(inv, context, slot) || player.itemAnimation != 0 || player.itemTime != 0)
            {
                // RETURN EARLY for all other items other than Adventure Mirror
                // Meaning we allow ItemSlot use while AM is the held item.
                if (player.HeldItem.type != ModContent.ItemType<AdventureMirror>())
                {
                    return;
                }

            }
        }
        int num = ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem);
        if (num != 3 && !flag)
        {
            return;
        }
        switch (num)
        {
            case 0:
                {
                    if (context == 6 && Main.mouseItem.type != 0)
                    {
                        inv[slot].SetDefaults();
                    }
                    if ((ItemSlot.IsAccessoryContext(context) && !ItemLoader.CanEquipAccessory(inv[slot], slot, context < 0)) || (context == 11 && !inv[slot].FitsAccessoryVanitySlot) || (context < 0 && !LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(slot, inv[slot], context)))
                    {
                        break;
                    }
                    if (Main.mouseItem.maxStack <= 1 || inv[slot].type != Main.mouseItem.type || inv[slot].stack == inv[slot].maxStack || Main.mouseItem.stack == Main.mouseItem.maxStack)
                    {
                        Utils.Swap(ref inv[slot], ref Main.mouseItem);
                    }
                    if (inv[slot].stack > 0)
                    {
                        ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], 21, context, inv[slot].stack));
                    }
                    else
                    {
                        ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(Main.mouseItem, context, 21, Main.mouseItem.stack));
                    }
                    if (inv[slot].stack > 0)
                    {
                        switch (Math.Abs(context))
                        {
                            case 0:
                                AchievementsHelper.NotifyItemPickup(player, inv[slot]);
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 16:
                            case 17:
                            case 25:
                            case 27:
                            case 33:
                                AchievementsHelper.HandleOnEquip(player, inv[slot], context);
                                break;
                        }
                    }
                    if (inv[slot].type == 0 || inv[slot].stack < 1)
                    {
                        inv[slot] = new Item();
                    }
                    if (Main.mouseItem.IsTheSameAs(inv[slot]) && inv[slot].stack != inv[slot].maxStack && Main.mouseItem.stack != Main.mouseItem.maxStack && ItemLoader.TryStackItems(inv[slot], Main.mouseItem, out var numTransfered))
                    {
                        ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], 21, context, numTransfered));
                    }
                    if (Main.mouseItem.type == 0 || Main.mouseItem.stack < 1)
                    {
                        Main.mouseItem = new Item();
                    }
                    if (Main.mouseItem.type > 0 || inv[slot].type > 0)
                    {
                        Recipe.FindRecipes();
                        SoundEngine.PlaySound(7);
                    }
                    if (context == 3 && Main.netMode == 1)
                    {
                        NetMessage.SendData(32, -1, -1, null, player.chest, slot);
                    }
                    break;
                }
            case 1:
                if (Main.mouseItem.stack == 1 && Main.mouseItem.type > 0 && inv[slot].type > 0 && inv[slot].IsNotTheSameAs(Main.mouseItem) && (context != 11 || Main.mouseItem.FitsAccessoryVanitySlot))
                {
                    if ((ItemSlot.IsAccessoryContext(context) && !ItemLoader.CanEquipAccessory(Main.mouseItem, slot, context < 0)) || (Math.Abs(context) == 11 && !Main.mouseItem.FitsAccessoryVanitySlot) || (context < 0 && !LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(slot, Main.mouseItem, context)))
                    {
                        break;
                    }
                    Utils.Swap(ref inv[slot], ref Main.mouseItem);
                    SoundEngine.PlaySound(7);
                    if (inv[slot].stack > 0)
                    {
                        switch (Math.Abs(context))
                        {
                            case 0:
                                AchievementsHelper.NotifyItemPickup(player, inv[slot]);
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 16:
                            case 17:
                            case 25:
                            case 27:
                            case 33:
                                AchievementsHelper.HandleOnEquip(player, inv[slot], context);
                                break;
                        }
                    }
                }
                else if (Main.mouseItem.type == 0 && inv[slot].type > 0)
                {
                    Utils.Swap(ref inv[slot], ref Main.mouseItem);
                    if (inv[slot].type == 0 || inv[slot].stack < 1)
                    {
                        inv[slot] = new Item();
                    }
                    if (Main.mouseItem.type == 0 || Main.mouseItem.stack < 1)
                    {
                        Main.mouseItem = new Item();
                    }
                    if (Main.mouseItem.type > 0 || inv[slot].type > 0)
                    {
                        Recipe.FindRecipes();
                        SoundEngine.PlaySound(7);
                    }
                }
                else if (Main.mouseItem.type > 0 && inv[slot].type == 0 && (context != 11 || Main.mouseItem.FitsAccessoryVanitySlot))
                {
                    if ((ItemSlot.IsAccessoryContext(context) && !ItemLoader.CanEquipAccessory(Main.mouseItem, slot, context < 0)) || (Math.Abs(context) == 11 && !Main.mouseItem.FitsAccessoryVanitySlot) || (context < 0 && !LoaderManager.Get<AccessorySlotLoader>().CanAcceptItem(slot, Main.mouseItem, context)))
                    {
                        break;
                    }
                    inv[slot] = ItemLoader.TransferWithLimit(Main.mouseItem, 1);
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(7);
                    if (inv[slot].stack > 0)
                    {
                        switch (Math.Abs(context))
                        {
                            case 0:
                                AchievementsHelper.NotifyItemPickup(player, inv[slot]);
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 16:
                            case 17:
                            case 25:
                            case 27:
                            case 33:
                                AchievementsHelper.HandleOnEquip(player, inv[slot], context);
                                break;
                        }
                    }
                }
                if ((context == 23 || context == 24) && Main.netMode == 1)
                {
                    NetMessage.SendData(121, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot);
                }
                if (context == 26 && Main.netMode == 1)
                {
                    NetMessage.SendData(124, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot);
                }
                break;
            case 2:
                if (Main.mouseItem.stack == 1 && Main.mouseItem.dye > 0 && inv[slot].type > 0 && inv[slot].type != Main.mouseItem.type)
                {
                    Utils.Swap(ref inv[slot], ref Main.mouseItem);
                    SoundEngine.PlaySound(7);
                    if (inv[slot].stack > 0)
                    {
                        switch (Math.Abs(context))
                        {
                            case 0:
                                AchievementsHelper.NotifyItemPickup(player, inv[slot]);
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 16:
                            case 17:
                            case 25:
                            case 27:
                            case 33:
                                AchievementsHelper.HandleOnEquip(player, inv[slot], context);
                                break;
                        }
                    }
                }
                else if (Main.mouseItem.type == 0 && inv[slot].type > 0)
                {
                    Utils.Swap(ref inv[slot], ref Main.mouseItem);
                    if (inv[slot].type == 0 || inv[slot].stack < 1)
                    {
                        inv[slot] = new Item();
                    }
                    if (Main.mouseItem.type == 0 || Main.mouseItem.stack < 1)
                    {
                        Main.mouseItem = new Item();
                    }
                    if (Main.mouseItem.type > 0 || inv[slot].type > 0)
                    {
                        Recipe.FindRecipes();
                        SoundEngine.PlaySound(7);
                    }
                }
                else if (Main.mouseItem.dye > 0 && inv[slot].type == 0)
                {
                    inv[slot] = ItemLoader.TransferWithLimit(Main.mouseItem, 1);
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(7);
                    if (inv[slot].stack > 0)
                    {
                        switch (Math.Abs(context))
                        {
                            case 0:
                                AchievementsHelper.NotifyItemPickup(player, inv[slot]);
                                break;
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 16:
                            case 17:
                            case 25:
                            case 27:
                            case 33:
                                AchievementsHelper.HandleOnEquip(player, inv[slot], context);
                                break;
                        }
                    }
                }
                if (context == 25 && Main.netMode == 1)
                {
                    NetMessage.SendData(121, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f);
                }
                if (context == 27 && Main.netMode == 1)
                {
                    NetMessage.SendData(124, -1, -1, null, Main.myPlayer, player.tileEntityAnchor.interactEntityID, slot, 1f);
                }
                break;
            case 3:
                ItemSlot.HandleShopSlot(inv, slot, rightClickIsValid: false, leftClickIsValid: true);
                break;
            case 4:
                if (PlayerLoader.CanSellItem(player, player.TalkNPC, inv, Main.mouseItem))
                {
                    Chest chest = Main.instance.shop[Main.npcShop];
                    if (player.SellItem(Main.mouseItem))
                    {
                        int soldItemIndex = chest.AddItemToShop(Main.mouseItem);
                        Main.mouseItem.SetDefaults();
                        SoundEngine.PlaySound(18);
                        ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], 21, 15));
                        PlayerLoader.PostSellItem(player, player.TalkNPC, chest.item, chest.item[soldItemIndex]);
                    }
                    else if (Main.mouseItem.value == 0)
                    {
                        int soldItemIndex2 = chest.AddItemToShop(Main.mouseItem);
                        Main.mouseItem.SetDefaults();
                        SoundEngine.PlaySound(7);
                        ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], 21, 15));
                        PlayerLoader.PostSellItem(player, player.TalkNPC, chest.item, chest.item[soldItemIndex2]);
                    }
                    Recipe.FindRecipes();
                    Main.stackSplit = 9999;
                }
                break;
            case 5:
                if (Main.mouseItem.IsAir)
                {
                    SoundEngine.PlaySound(7);
                    Main.mouseItem = inv[slot].Clone();
                    Main.mouseItem.stack = Main.mouseItem.maxStack;
                    Main.mouseItem.OnCreated(new JourneyDuplicationItemCreationContext());
                    ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(inv[slot], 29, 21));
                }
                break;
        }
        if ((uint)context > 2u && context != 5 && context != 32)
        {
            inv[slot].favorited = false;
        }
    }

    // SELLING TO NPCS
    private static bool Hook_SellItem(
        On_Player.orig_SellItem orig,
        Player self,
        Item item,
        int stack)
    {
        if (IsAdventureMirror(item))
        {
            //if (Main.mouseLeft && Main.mouseLeftRelease)
                //PopupTextHelper.NewText("Cannot sell Adventure Mirror!", self);

            return false;
        }

        return orig(self, item, stack);
    }
}