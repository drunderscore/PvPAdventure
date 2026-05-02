using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator.TeammateOverlay.Drawers;

public static class HotbarDrawer
{
    private const int SpectatedHotbarDrawContext = ItemSlot.Context.HotbarItem;
    private static bool ownedHotbarHover;

    public static void ClearOwnedHover()
    {
        if (!ownedHotbarHover)
            return;

        Main.HoverItem = new Item();
        Main.hoverItemName = "";
        Main.mouseText = false;
        Main.rare = 0;
        ownedHotbarHover = false;
    }

    public static void DrawHotbar(Player player)
    {
        if (player?.active != true)
            return;

        bool ownsHotbarHoverThisFrame = false;
        Color oldInventoryBack = Main.inventoryBack;

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        int oldSelectedItem = local.selectedItem;

        try
        {
            local.selectedItem = player.selectedItem;

            if (Main.playerInventory)
                return;

            string text = Lang.inter[37].Value;

            if (!string.IsNullOrEmpty(player.inventory[player.selectedItem].Name))
                text = player.inventory[player.selectedItem].AffixName();

            DynamicSpriteFontExtensionMethods.DrawString(
                position: new Vector2(236f - (FontAssets.MouseText.Value.MeasureString(text) / 2f).X, 0f),
                spriteBatch: Main.spriteBatch,
                spriteFont: FontAssets.MouseText.Value,
                text: text,
                color: new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor),
                rotation: 0f,
                origin: default,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );

            int num = 20;

            for (int i = 0; i < 10; i++)
            {
                if (i == player.selectedItem)
                {
                    if (Main.hotbarScale[i] < 1f)
                        Main.hotbarScale[i] += 0.05f;
                }
                else if (Main.hotbarScale[i] > 0.75f)
                {
                    Main.hotbarScale[i] -= 0.05f;
                }

                float scale = Main.hotbarScale[i];
                int y = (int)(20f + 22f * (1f - scale));
                int alpha = (int)(75f + 150f * scale);
                Color lightColor = new(255, 255, 255, alpha);

                if (!player.hbLocked && !PlayerInput.IgnoreMouseInterface && Main.mouseX >= num && Main.mouseX <= num + TextureAssets.InventoryBack.Width() * scale && Main.mouseY >= y && Main.mouseY <= y + TextureAssets.InventoryBack.Height() * scale && !player.channel)
                {
                    ownsHotbarHoverThisFrame = true;
                    Main.LocalPlayer.mouseInterface = true;
                    Main.hoverItemName = player.inventory[i].AffixName();

                    if (player.inventory[i].stack > 1)
                        Main.hoverItemName += " (" + player.inventory[i].stack + ")";

                    Main.rare = player.inventory[i].rare;
                    Main.mouseText = true;
                }

                float oldScale = Main.inventoryScale;
                Main.inventoryScale = scale;

                ItemSlot.Draw(Main.spriteBatch, player.inventory, SpectatedHotbarDrawContext, i, new Vector2(num, y), lightColor);

                Main.inventoryScale = oldScale;
                num += (int)(TextureAssets.InventoryBack.Width() * scale) + 4;
            }

            int selectedItem = player.selectedItem;

            if (selectedItem >= 10 && (selectedItem != 58 || Main.mouseItem.type > 0))
            {
                float oldScale = Main.inventoryScale;
                Main.inventoryScale = 1f;

                int y = 20;
                Color lightColor = new(255, 255, 255, 225);
                ItemSlot.Draw(Main.spriteBatch, player.inventory, SpectatedHotbarDrawContext, selectedItem, new Vector2(num, y), lightColor);

                Main.inventoryScale = oldScale;
            }
        }
        finally
        {
            local.selectedItem = oldSelectedItem;
            Main.inventoryBack = oldInventoryBack;

            if (ownedHotbarHover && !ownsHotbarHoverThisFrame)
                ClearOwnedHover();

            ownedHotbarHover = ownsHotbarHoverThisFrame;
        }
    }

    [Obsolete("Try this later")]
    public static void DrawHotbar2(Player player)
    {
        if (Main.playerInventory || player?.active != true)
            return;

        Color oldInventoryBack = Main.inventoryBack;
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

            Main.inventoryBack = i == player.selectedItem ? Color.Yellow : oldInventoryBack;
            ItemSlot.Draw(Main.spriteBatch, player.inventory, SpectatedHotbarDrawContext, i, new Vector2(x, y), lightColor);
            Main.inventoryBack = oldInventoryBack;

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
            Main.inventoryBack = Color.Yellow;

            ItemSlot.Draw(Main.spriteBatch, player.inventory, SpectatedHotbarDrawContext, selectedItem, new Vector2(x, y), lightColor);

            Main.inventoryBack = oldInventoryBack;
            Main.inventoryScale = oldInventoryScale;
        }
    }
}
