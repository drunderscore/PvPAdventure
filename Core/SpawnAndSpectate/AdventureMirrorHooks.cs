using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using PvPAdventure.Common.Debug;
using PvPAdventure.Content.Items;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Prevents the Adventure Mirror from being removed from the player inventory via trashing, selling etc.
/// Various inventory hooks are used to achieve this.
/// Also allows using the Adventure Mirror even when item animation is active with an ignore mouse hook.
/// </summary>
public class AdventureMirrorHooks : ModSystem
{
    // Manual hook used to ignore mouse interface check while using Adventure Mirror
    private Hook ignoreMouseHook;

    public override void Load()
    {
        // Detours
        On_ItemSlot.LeftClick_SellOrTrash += Modify_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int += Modify_LeftClick_ItemArray;
        On_ItemSlot.RightClick_ItemArray_int_int += Modify_RightClick;
        On_Player.SellItem += Modify_SellItem;
        On_Player.DropSelectedItem += Modify_DropSelectedItem;

        // Mouse interface getter hook called IgnoreMouseInterface
        MethodInfo getter = typeof(PlayerInput).GetMethod("get_IgnoreMouseInterface", BindingFlags.Public | BindingFlags.Static);
        if (getter != null)
            ignoreMouseHook = new Hook(getter, OverrideIgnoreMouseInterface);
        else
            Log.Error("PlayerInput.get_IgnoreMouseInterface not found!");
    }

    public override void Unload()
    {
        On_ItemSlot.LeftClick_SellOrTrash -= Modify_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int -= Modify_LeftClick_ItemArray;
        On_ItemSlot.RightClick_ItemArray_int_int -= Modify_RightClick;
        On_Player.SellItem -= Modify_SellItem;
        On_Player.DropSelectedItem -= Modify_DropSelectedItem;

        ignoreMouseHook?.Dispose();
    }

    #region Helpers
    /// <summary> Identify Adventure Mirror item. </summary>
    private static bool IsAdventureMirror(Item item) => !item.IsAir && item.type == ModContent.ItemType<AdventureMirror>();

    /// <summary> Shows popup text above the local player. </summary>
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
    #endregion

    private bool OverrideIgnoreMouseInterface(Func<bool> orig)
    {
        Player p = Main.LocalPlayer;

        if (p != null &&
            Main.playerInventory &&
            p.itemAnimation > 0 &&
            p.HeldItem.type == ModContent.ItemType<AdventureMirror>())
        {
            return false; // allow UI interaction while Adventure Mirror is animating
        }

        return orig();
    }

    // Vanilla method with the item animation check removed for Adventure Mirror.
    private static void Modify_RightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig,Item[] inv, int context, int slot)
    {
        Player player = Main.player[Main.myPlayer];
        inv[slot].newAndShiny = false;

        // Bypass item animation check for Adventure Mirror here!
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
                            if (((Main.mouseItem.IsTheSameAs(inv[slot]) && ItemLoader.CanStack(Main.mouseItem, inv[slot])) || Main.mouseItem.type == ItemID.None) && (Main.mouseItem.stack < Main.mouseItem.maxStack || Main.mouseItem.type == ItemID.None))
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

    private void Modify_DropSelectedItem(On_Player.orig_DropSelectedItem orig,Player self)
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
    private static bool Modify_SellItem(On_Player.orig_SellItem orig,Player self,Item item,int stack)
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
    private static bool Modify_LeftClick_SellOrTrash(On_ItemSlot.orig_LeftClick_SellOrTrash orig,Item[] inv, int context, int slot)
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
    // Dragging onto trash slot / chests / banks
    /// </summary>
    private static void Modify_LeftClick_ItemArray(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,Item[] inv, int context, int slot)
    {
        // Disallow unfavorite for AdventureMirror (Alt + LeftClick)
        if (slot >= 0 && slot < inv.Length && IsAdventureMirror(inv[slot]))
        {
            if (Main.keyState.IsKeyDown(Main.FavoriteKey) && Main.mouseLeft && Main.mouseLeftRelease)
            {
                inv[slot].favorited = true;

                Popup("Mods.PvPAdventure.AdventureMirror.CannotUnfavorite");
                return; // swallow favorite toggle
            }
        }

        // Block dragging mirror onto trash
        if (context == ItemSlot.Context.TrashItem && IsAdventureMirror(Main.mouseItem))
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotTrash");
            return;
        }

        // Block dragging mirror onto shop
        if (context == ItemSlot.Context.ShopItem && IsAdventureMirror(Main.mouseItem))
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotSell");
            return;
        }

        // Block dragging mirror onto chest or bank
        if (context == ItemSlot.Context.ChestItem && IsAdventureMirror(Main.mouseItem)
            || context == ItemSlot.Context.BankItem && IsAdventureMirror(Main.mouseItem))
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                Popup("Mods.PvPAdventure.AdventureMirror.CannotStore");
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

internal sealed class DisablePickupWhileHoldingMirror : GlobalItem
{
    public override bool CanPickup(Item item, Player player)
    {
        if (player == null || !player.active)
        {
            return true;
        }

        if (item.IsACoin || item.ammo != 0)
        {
            return true; // always allow coins and ammo
        }

        int freeSlots = 0;

        // Count free inventory slots: 0–49
        for (int i = 0; i < 50; i++)
        {
            if (player.inventory[i].IsAir)
            {
                freeSlots++;
            }
        }

        // If Adventure Mirror is held and player has 1 or less inventory slots, block all item pickup in the world
        // This effectively forces the player to always keep at least one free slot when holding the mirror.
        if (player.HeldItem.type == ModContent.ItemType<AdventureMirror>() && freeSlots <= 1)
        {
            return false;
        }

        return true;
    }
}
