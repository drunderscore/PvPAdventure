using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using PvPAdventure.Content.Items;

public class ItemSlotHooks : ModSystem
{
    public override void Load()
    {
        // 1) Handles Shift+trash / quick sell path
        On_ItemSlot.LeftClick_SellOrTrash += Hook_LeftClick_SellOrTrash;

        // 2) Handles normal click on the trash slot
        On_ItemSlot.LeftClick_ItemArray_int_int += Hook_LeftClick_ItemArray;

        // 3) Handles ALL selling (manual and quick) via Player.SellItem
        On_Player.SellItem += Hook_SellItem;
    }

    public override void Unload()
    {
        On_ItemSlot.LeftClick_SellOrTrash -= Hook_LeftClick_SellOrTrash;
        On_ItemSlot.LeftClick_ItemArray_int_int -= Hook_LeftClick_ItemArray;
        On_Player.SellItem -= Hook_SellItem;
    }

    // ======== QUICK TRASH / QUICK SELL (Shift + click) ========
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

    // ======== DRAGGING ONTO THE TRASH SLOT ========
    private static void Hook_LeftClick_ItemArray(
        On_ItemSlot.orig_LeftClick_ItemArray_int_int orig,
        Item[] inv, int context, int slot)
    {
        if (context == ItemSlot.Context.TrashItem
            && !Main.mouseItem.IsAir
            && Main.mouseItem.type == ModContent.ItemType<AdventureMirror>())
        {
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return; // do NOT process the trash slot
        }

        orig(inv, context, slot);
    }

    // ======== ANY SELL TO NPC SHOP (manual or quick) ========
    private static bool Hook_SellItem(
        On_Player.orig_SellItem orig,
        Player self,
        Item item,
        int stack)
    {
        if (!item.IsAir && item.type == ModContent.ItemType<AdventureMirror>())
        {
            //SoundEngine.PlaySound(SoundID.MenuClose);
            return false; // selling fails, nothing is removed
        }

        return orig(self, item, stack);
    }
}
