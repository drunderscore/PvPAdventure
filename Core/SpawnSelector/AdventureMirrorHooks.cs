using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector;

/// <summary>
/// Prevents the Adventure Mirror from being removed from the player inventory via trashing, selling etc.
/// Various inventory hooks are used to achieve this.
/// Also allows using the Adventure Mirror even when item animation is active together with <see cref="InventoryWhileUsingItemSystem"/>
/// </summary>
public class AdventureMirrorHooks : ModSystem
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
        On_ItemSlot.RightClick_ItemArray_int_int -= Hook_RightClick;
        On_Player.SellItem -= Hook_SellItem;
        On_Player.DropSelectedItem -= Hook_DropSelectedItem;
        On_Player.dropItemCheck -= Hook_DropItemCheck;
    }
    // Helper to identify Adventure Mirror
    private static bool IsAdventureMirror(Item item) => !item.IsAir && item.type == ModContent.ItemType<AdventureMirror>();

    // Helper to show popup text
    private static void Popup(string key)
    {
        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = Color.Crimson,
            Text = Language.GetTextValue(key),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 60 * 2
        }, Main.LocalPlayer.Top + new Vector2(0f, -40f));
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
            if (Main.mouseRight && Main.mouseRightRelease)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.CannotDrop"),
                    Velocity = new(0.0f, 4.0f),
                    DurationInFrames = 60 * 2
                }, Main.LocalPlayer.Top + new Vector2(0, -40));
            }
            
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

    /// <summary>
    /// Block selling Adventure Mirror to NPC's
    /// </summary>
    private static bool Hook_SellItem(On_Player.orig_SellItem orig,Player self,Item item,int stack)
    {
        if (IsAdventureMirror(item))
        {
            return false;
        }

        return orig(self, item, stack);
    }

    /// <summary>
    /// Block quick trash/quick sell (shift+click) Adventure Mirror
    /// </summary>
    private static bool Hook_LeftClick_SellOrTrash(On_ItemSlot.orig_LeftClick_SellOrTrash orig,Item[] inv, int context, int slot)
    {
        Item item = inv[slot];

        if (!item.IsAir && item.type == ModContent.ItemType<AdventureMirror>())
        {
            //if (Main.npcShop > 0)
                //Popup("Mods.PvPAdventure.AdventureMirror.CannotTrash");
            return false; // skip completely
        }

        return orig(inv, context, slot);
    }

    /// <summary>
    /// Vanilla method with the item animation check removed for Adventure Mirror.
    // Draggong onto trash slot / chests / banks
    /// </summary>
    private static void Hook_LeftClick_ItemArray(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,Item[] inv, int context, int slot)
    {
        // Disallow unfavorite for AdventureMirror (Alt + LeftClick)
        if (slot >= 0 && slot < inv.Length && IsAdventureMirror(inv[slot]))
        {
            bool favoriteModifierDown = Main.keyState.IsKeyDown(Main.FavoriteKey);
            bool leftClick = Main.mouseLeft && Main.mouseLeftRelease;

            if (favoriteModifierDown && leftClick)
            {
                inv[slot].favorited = true;

                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.CannotUnfavorite"),
                    Velocity = new Vector2(0f, -4f),
                    DurationInFrames = 60 * 2
                }, Main.LocalPlayer.Top + new Vector2(0, -4));

                return; // swallow favorite toggle
            }
        }

        bool isMirrorOnMouse = IsAdventureMirror(Main.mouseItem);

        // Block putting mirror into storage
        bool isStorageSlot = context == ItemSlot.Context.ChestItem || context == ItemSlot.Context.BankItem;

        if (isStorageSlot && isMirrorOnMouse)
            return;

        // Block dragging mirror onto trash
        if (context == ItemSlot.Context.TrashItem && isMirrorOnMouse)
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotTrash");
            return;
        }

        // Block dragging mirror onto shop
        if (context == ItemSlot.Context.ShopItem && isMirrorOnMouse)
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotSell");
            return;
        }

        // Block dragging mirror onto chest
        if (context == ItemSlot.Context.ShopItem && isMirrorOnMouse)
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotSell");
            return;
        }

        // Vanilla (orig)
        Player player = Main.LocalPlayer;

        bool bypassAnimCheck = player.HeldItem.type == ModContent.ItemType<AdventureMirror>();

        int oldAnim = player.itemAnimation;
        int oldTime = player.itemTime;

        if (bypassAnimCheck)
        {
            player.itemAnimation = 0;
            player.itemTime = 0;
        }

        try
        {
            orig(inv, context, slot);
        }
        finally
        {
            if (bypassAnimCheck)
            {
                player.itemAnimation = oldAnim;
                player.itemTime = oldTime;
            }
        }
    }

}