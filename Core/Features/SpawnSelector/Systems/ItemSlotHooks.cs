using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using PvPAdventure.System.Client;
using Microsoft.Xna.Framework.Input;

namespace PvPAdventure.Core.Features.SpawnSelector.Systems;

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
    }

    public override void Unload()
    {
        On_ItemSlot.LeftClick_SellOrTrash -= Hook_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int -= Hook_LeftClick_ItemArray;
        On_Player.SellItem -= Hook_SellItem;
    }

    // QUICK TRASH / QUICK SELL (Shift + click)
    private static bool Hook_LeftClick_SellOrTrash(
        On_ItemSlot.orig_LeftClick_SellOrTrash orig,
        Item[] inv, int context, int slot)
    {
        Item item = inv[slot];

        if (!item.IsAir && item.type == ModContent.ItemType<AdventureMirror>())
        {
            if (Main.keyState.IsKeyDown(Keys.LeftShift) && Main.mouseLeft && Main.mouseLeftRelease)
            {
                PopupTextHelper.NewText("Cannot quick trash Adventure Mirror!");
            }
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return false; // skip SellOrTrash completely
        }

        return orig(inv, context, slot);
    }

    private static bool IsAdventureMirror(Item item)
            => !item.IsAir && item.type == ModContent.ItemType<AdventureMirror>();

    // DRAGGING ONTO THE TRASH SLOT
    private static void Hook_LeftClick_ItemArray(
        On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,
        Item[] inv, int context, int slot)
    {
        // 1) Hard block: putting mirror into any chest/bank slot
        bool isStorageSlot =
            context == ItemSlot.Context.ChestItem ||
            context == ItemSlot.Context.BankItem;   // Piggy bank

        if (isStorageSlot &&
            IsAdventureMirror(Main.mouseItem)) // we are dragging the mirror onto this slot
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
                PopupTextHelper.NewText("Cannot store Adventure Mirror!");

            // Do NOT call orig -> cancel the placement
            return;
        }

        // Fallback to vanilla behavior
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
            if (Main.mouseLeft && Main.mouseLeftRelease)
                PopupTextHelper.NewText("Cannot sell Adventure Mirror!");

            return false;
        }

        return orig(self, item, stack);
    }
}