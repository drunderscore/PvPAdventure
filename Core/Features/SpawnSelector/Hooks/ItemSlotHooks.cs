using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using PvPAdventure.Content.Items;
using Terraria.Audio;
using Terraria.ID;
using Microsoft.Xna.Framework;

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
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return false; // skip SellOrTrash completely
        }

        return orig(inv, context, slot);
    }

    // DRAGGING ONTO THE TRASH SLOT
    private static void Hook_LeftClick_ItemArray(
        On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,
        Item[] inv, int context, int slot)
    {

        if (context == ItemSlot.Context.TrashItem
            && !Main.mouseItem.IsAir
            && Main.mouseItem.type == ModContent.ItemType<AdventureMirror>())
        {

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "Cannot trash Adventure Mirror!",
                    Velocity = new(0f, -4f),
                    DurationInFrames = 60
                }, Main.LocalPlayer.Top);
            }

            return; 
        }

        orig(inv, context, slot);
    }

    // SELLING TO NPCS
    private static bool Hook_SellItem(
        On_Player.orig_SellItem orig,
        Player self,
        Item item,
        int stack)
    {
        if (!item.IsAir && item.type == ModContent.ItemType<AdventureMirror>())
        {
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return false; 
        }

        return orig(self, item, stack);
    }
}
