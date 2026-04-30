using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

public static class HotbarDrawer
{
    private static bool ownedHotbarHover;
    public static void DrawHotbar(Player player)
    {
        bool ownsHotbarHoverThisFrame = false;
        Color oldInventoryBack = Main.inventoryBack;

        try
        {
            if (Main.playerInventory /*|| player.ghost*/) // target isnt ghost so redundant but w/e.
            {
                return;
            }
            string text = Lang.inter[37].Value;
            if (player.inventory[player.selectedItem].Name != null && player.inventory[player.selectedItem].Name != "")
            {
                text = player.inventory[player.selectedItem].AffixName();
            }
            DynamicSpriteFontExtensionMethods.DrawString(position: new Vector2(236f - (FontAssets.MouseText.Value.MeasureString(text) / 2f).X, 0f), spriteBatch: Main.spriteBatch, spriteFont: FontAssets.MouseText.Value, text: text, color: new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), rotation: 0f, origin: default(Vector2), scale: 1f, effects: SpriteEffects.None, layerDepth: 0f);
            int num = 20;
            for (int i = 0; i < 10; i++)
            {
                if (i == player.selectedItem)
                {
                    if (Main.hotbarScale[i] < 1f)
                    {
                        Main.hotbarScale[i] += 0.05f;
                    }
                }
                else if ((double)Main.hotbarScale[i] > 0.75)
                {
                    Main.hotbarScale[i] -= 0.05f;
                }
                float num2 = Main.hotbarScale[i];
                int num3 = (int)(20f + 22f * (1f - num2));
                int a = (int)(75f + 150f * num2);
                Color lightColor = new Color(255, 255, 255, a);
                if (!player.hbLocked && !PlayerInput.IgnoreMouseInterface && Main.mouseX >= num && (float)Main.mouseX <= (float)num + (float)TextureAssets.InventoryBack.Width() * Main.hotbarScale[i] && Main.mouseY >= num3 && (float)Main.mouseY <= (float)num3 + (float)TextureAssets.InventoryBack.Height() * Main.hotbarScale[i] && !player.channel)
                {
                    ownsHotbarHoverThisFrame = true;
                    Main.LocalPlayer.mouseInterface = true;
                    player.mouseInterface = true;
                    player.cursorItemIconEnabled = false;
                    //if (Main.mouseLeft && !player.hbLocked && !Main.blockMouse)
                    //{
                    //    player.changeItem = i;
                    //}
                    Main.hoverItemName = player.inventory[i].AffixName();
                    if (player.inventory[i].stack > 1)
                    {
                        Main.hoverItemName = Main.hoverItemName + " (" + player.inventory[i].stack + ")";
                    }
                    Main.rare = player.inventory[i].rare;
                }
                float num4 = Main.inventoryScale;
                Main.inventoryScale = num2;
                Main.inventoryBack = i == player.selectedItem ? Color.Yellow : oldInventoryBack;
                // --- Actual draw call ---
                ItemSlot.Draw(Main.spriteBatch, player.inventory, 13, i, new Vector2(num, num3), lightColor);
                Main.inventoryBack = oldInventoryBack;
                Main.inventoryScale = num4;
                num += (int)((float)TextureAssets.InventoryBack.Width() * Main.hotbarScale[i]) + 4;
            }
            int selectedItem = player.selectedItem;
            if (selectedItem >= 10 && (selectedItem != 58 || Main.mouseItem.type > 0))
            {
                float num5 = 1f;
                int num6 = (int)(20f + 22f * (1f - num5));
                int a2 = (int)(75f + 150f * num5);
                Color lightColor2 = new Color(255, 255, 255, a2);
                float num7 = Main.inventoryScale;
                Main.inventoryScale = num5;
                Main.inventoryBack = Color.Yellow;
                ItemSlot.Draw(Main.spriteBatch, player.inventory, 13, selectedItem, new Vector2(num, num6), lightColor2);
                Main.inventoryBack = oldInventoryBack;
                Main.inventoryScale = num7;
            }
        }
        finally
        {
            Main.inventoryBack = oldInventoryBack;

            if (ownedHotbarHover && !ownsHotbarHoverThisFrame)
            {
                Main.HoverItem = new Item();
                Main.hoverItemName = "";
                Main.mouseText = false;
                Main.rare = 0;
            }

            ownedHotbarHover = ownsHotbarHoverThisFrame;
        }
    }

    [Obsolete("Try this later")]
    public static void DrawHotbar2(Player player)
    {
        if (Main.playerInventory || player?.active != true)
            return;

        string text = Lang.inter[37].Value;

        if (!string.IsNullOrEmpty(player.inventory[player.selectedItem].Name))
            text = player.inventory[player.selectedItem].AffixName();

        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(236f - FontAssets.MouseText.Value.MeasureString(text).X * 0.5f, 0f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor));

        int x = 20;

        for (int i = 0; i < 10; i++)
        {
            float slotScale = i == player.selectedItem ? 1f : 0.75f;
            int y = (int)(20f + 22f * (1f - slotScale));
            int alpha = (int)(75f + 150f * slotScale);
            Color lightColor = new(255, 255, 255, alpha);

            if (!PlayerInput.IgnoreMouseInterface &&
                Main.mouseX >= x &&
                Main.mouseX <= x + TextureAssets.InventoryBack.Width() * slotScale &&
                Main.mouseY >= y &&
                Main.mouseY <= y + TextureAssets.InventoryBack.Height() * slotScale)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.hoverItemName = player.inventory[i].AffixName();

                if (player.inventory[i].stack > 1)
                    Main.hoverItemName += " (" + player.inventory[i].stack + ")";

                Main.rare = player.inventory[i].rare;
            }

            float oldInventoryScale = Main.inventoryScale;
            Main.inventoryScale = slotScale;

            ItemSlot.Draw(Main.spriteBatch, player.inventory, 13, i, new Vector2(x, y), lightColor);

            Main.inventoryScale = oldInventoryScale;
            x += (int)(TextureAssets.InventoryBack.Width() * slotScale) + 4;
        }

        int selectedItem = player.selectedItem;

        if (selectedItem >= 10 && (selectedItem != 58 || Main.mouseItem.type > 0))
        {
            float slotScale = 1f;
            int y = (int)(20f + 22f * (1f - slotScale));
            Color lightColor = new(255, 255, 255, 225);

            float oldInventoryScale = Main.inventoryScale;
            Main.inventoryScale = slotScale;

            ItemSlot.Draw(Main.spriteBatch, player.inventory, 13, selectedItem, new Vector2(x, y), lightColor);

            Main.inventoryScale = oldInventoryScale;
        }
    }
}
