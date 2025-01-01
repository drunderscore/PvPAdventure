using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class AdventureInventory : ModSystem
{
    private delegate void AccessorySlotLoaderDrawSlotDelegate(AccessorySlotLoader self, Item[] items, int context,
        int slot, bool flag3, int xLoc, int yLoc, bool skipCheck);

    private Hook _accessorySlotLoaderDrawHook;

    public override void Load()
    {
        // TML handles the accessory slots specially, because it's expected mods will want to add their own.
        // This here will both draw and handle the slot.
        _accessorySlotLoaderDrawHook =
            new Hook(
                typeof(AccessorySlotLoader).GetMethod("DrawSlot", BindingFlags.NonPublic | BindingFlags.Instance),
                OnAccessorySlotLoaderDrawSlot);

        // Otherwise, slot drawing and handling will end up in these two functions.
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += OnItemSlotDraw;
        On_ItemSlot.Handle_ItemArray_int_int += OnItemSlotHandle;

        // However, sometimes the handling logic will be manually inlined (armor dye slots), so we need to check the
        // individual methods that implement the lower level handling.
        On_ItemSlot.OverrideHover_ItemArray_int_int += OnItemSlotOverrideHover;
        On_ItemSlot.RightClick_ItemArray_int_int += OnItemSlotRightClick;
        On_ItemSlot.LeftClick_ItemArray_int_int += OnItemSlotLeftClick;
        On_ItemSlot.MouseHover_ItemArray_int_int += OnItemSlotMouseHover;
    }

    private static bool IsPlayerDyeContext(int context) => context is ItemSlot.Context.EquipDye
        or ItemSlot.Context.EquipMiscDye or ItemSlot.Context.ModdedDyeSlot;

    private void OnAccessorySlotLoaderDrawSlot(AccessorySlotLoaderDrawSlotDelegate orig, AccessorySlotLoader self,
        Item[] items, int context, int slot, bool flag3, int xLoc, int yLoc, bool skipCheck)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(self, items, context, slot, flag3, xLoc, yLoc, skipCheck);
    }

    private void OnItemSlotDraw(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig,
        SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(spriteBatch, inv, context, slot, position, lightColor);
    }

    private void OnItemSlotHandle(On_ItemSlot.orig_Handle_ItemArray_int_int orig, Item[] inv, int context, int slot)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(inv, context, slot);
    }

    private void OnItemSlotOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context,
        int slot)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(inv, context, slot);
    }

    private void OnItemSlotRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context,
        int slot)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(inv, context, slot);
    }

    private void OnItemSlotLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context,
        int slot)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(inv, context, slot);
    }

    private void OnItemSlotMouseHover(On_ItemSlot.orig_MouseHover_ItemArray_int_int orig, Item[] inv, int context,
        int slot)
    {
        if (IsPlayerDyeContext(context))
            return;

        orig(inv, context, slot);
    }

    // FIXME: Ignore client syncing dye slots to server.

    public override void Unload()
    {
        _accessorySlotLoaderDrawHook?.Dispose();
        _accessorySlotLoaderDrawHook = null;
    }
}