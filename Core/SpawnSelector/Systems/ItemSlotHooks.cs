using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using PvPAdventure.Content.Items;

namespace PvPAdventure.Core.SpawnSelector.Systems;

/// <summary>
/// Prevents the Adventure Mirror from being removed from the player inventory via trashing or selling.
/// </summary>
public class ItemSlotHooks : ModSystem
{
    public override void Load()
    {
        On_ItemSlot.LeftClick_SellOrTrash += Hook_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int += Hook_LeftClick_ItemArray;
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
                //PopupTextHelper.NewText("Cannot store Adventure Mirror!");

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
        orig(inv, context, slot);
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