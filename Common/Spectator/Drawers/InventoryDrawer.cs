using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.Spectator.Drawers;

public static class InventoryDrawer
{
    public static void DrawInventory(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        int size = Main.screenWidth < 1200 ? 36 : 40;
        int pad = 4;
        int rowStep = size + pad;

        Utils.DrawBorderStringBig(sb, $"{player.name}'s Inventory", new Vector2(start.X, start.Y - 12f), Color.White, 0.5f);

        Vector2 inventoryStart = new(start.X, start.Y + 20f);

        for (int i = 0; i < 50; i++)
        {
            int row = i / 10;
            int col = i % 10;
            Rectangle r = new(
                (int)(inventoryStart.X + col * rowStep),
                (int)(inventoryStart.Y + row * rowStep),
                size,
                size);

            if (r.Bottom > viewport.Bottom - 10)
                continue;

            Item item = player.inventory[i];

            if (item == player.HeldItem)
                sb.Draw(TextureAssets.InventoryBack14.Value, r, Color.White);
            else
                sb.Draw(TextureAssets.InventoryBack.Value, r, Color.White);

            if (!item.IsAir)
            {
                Vector2 center = new(r.X + r.Width / 2f, r.Y + r.Height / 2f);
                ItemSlot.DrawItemIcon(item, 31, sb, center, 0.9f, size - 6, Color.White);

                if (r.Contains(Main.MouseScreen.ToPoint()))
                {
                    UICommon.TooltipMouseText("");
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;

                    if (Main.mouseRight && Main.mouseRightRelease && !PlayerInput.IgnoreMouseInterface)
                    {
                        bool TryEquipFromInventory(Player p, int invIndex)
                        {
                            if (player != Main.LocalPlayer)
                                return false;

                            ref Item it = ref p.inventory[invIndex];

                            if (it.IsAir)
                                return false;

                            if (it.accessory)
                            {
                                int begin = 3;
                                int end = Math.Min(10, p.armor.Length);
                                int empty = -1;

                                for (int s = begin; s < end; s++)
                                {
                                    if (p.armor[s].type == it.type)
                                        return false;

                                    if (empty < 0 && p.armor[s].IsAir)
                                        empty = s;
                                }

                                if (empty >= 0)
                                {
                                    p.armor[empty] = it.Clone();
                                    it.TurnToAir();
                                    return true;
                                }

                                for (int s = begin; s < end; s++)
                                {
                                    if (!p.armor[s].IsAir)
                                    {
                                        Item tmp = p.armor[s];
                                        p.armor[s] = it;
                                        p.inventory[invIndex] = tmp;
                                        return true;
                                    }
                                }

                                return false;
                            }

                            int equip = -1;
                            int vanity = -1;

                            if (it.headSlot >= 0)
                            {
                                equip = 0;
                                vanity = 10;
                            }
                            else if (it.bodySlot >= 0)
                            {
                                equip = 1;
                                vanity = 11;
                            }
                            else if (it.legSlot >= 0)
                            {
                                equip = 2;
                                vanity = 12;
                            }

                            if (equip < 0)
                                return false;

                            int target = it.defense > 0 ? equip : vanity;

                            if (p.armor[target].IsAir)
                            {
                                p.armor[target] = it.Clone();
                                it.TurnToAir();
                                return true;
                            }

                            Item sw = p.armor[target];
                            p.armor[target] = it;
                            p.inventory[invIndex] = sw;
                            return true;
                        }

                        if (TryEquipFromInventory(player, i))
                            Main.mouseRightRelease = false;
                    }
                }
            }

            if (i < 10)
            {
                string label = i == 9 ? "0" : (i + 1).ToString();
                Vector2 numberPos = new(r.Right - 32f, r.Bottom - 36f);
                Utils.DrawBorderString(sb, label, numberPos, Color.White, 0.75f, 0f, 0f);
            }
        }

        Vector2 trashStart = new(inventoryStart.X + 9 * rowStep, inventoryStart.Y + 5 * rowStep);
        DrawTrash(sb, trashStart, player, viewport);

        Vector2 coinsStart = new(inventoryStart.X + 10 * rowStep + 10f, inventoryStart.Y + 22f);
        DrawCoins(sb, coinsStart, player, viewport);
        DrawAmmo(sb, new Vector2(coinsStart.X + 36f, coinsStart.Y), player, viewport);

        DrawBuffs(sb, new Vector2(inventoryStart.X, inventoryStart.Y + 5 * rowStep + 14f), player, viewport);
    }

    public static void DrawCoins(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        DrawSmallSlotGroup(sb, "Coins", start, player.inventory, ItemSlot.Context.InventoryCoin, 50, 4, player, viewport);
    }

    public static void DrawAmmo(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        DrawSmallSlotGroup(sb, "Ammo", start, player.inventory, ItemSlot.Context.InventoryAmmo, 54, 4, player, viewport);
    }

    private static void DrawSmallSlotGroup(SpriteBatch sb, string title, Vector2 start, Item[] items, int context, int firstSlot, int count, Player player, Rectangle viewport)
    {
        int size = Main.screenWidth < 1200 ? 27 : 30;
        int pad = 3;
        int rowStep = size + pad;

        const float titleScale = 0.68f;

        Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * titleScale;
        Vector2 titlePosition = new(start.X + size * 0.5f - titleSize.X * 0.5f, start.Y+2);

        Utils.DrawBorderString(sb, title, titlePosition, Color.White, titleScale);

        for (int i = 0; i < count; i++)
        {
            int slot = firstSlot + i;
            int x = (int)start.X;
            int y = (int)(start.Y + 18f + i * rowStep);

            if (slot < 0 || slot >= items.Length || y + size > viewport.Bottom)
                continue;

            DrawLoaderSlot(items, context, slot, x, y, player, size);
        }
    }

    private static void DrawTrash(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        int size = Main.screenWidth < 1200 ? 36 : 40;

        if (start.Y + size > viewport.Bottom)
            return;

        float oldScale = Main.inventoryScale;
        Main.inventoryScale = size / (float)TextureAssets.InventoryBack.Width();

        Vector2 position = new((int)start.X, (int)start.Y);
        Rectangle box = new((int)position.X, (int)position.Y, size, size);
        bool hover = box.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;

        if (hover)
        {
            Main.LocalPlayer.mouseInterface = true;

            if (!player.trashItem.IsAir)
            {
                UICommon.TooltipMouseText("");
                ItemSlot.OverrideHover(ref player.trashItem, ItemSlot.Context.TrashItem);
                ItemSlot.MouseHover(ref player.trashItem, ItemSlot.Context.TrashItem);
            }
        }

        ItemSlot.Draw(sb, ref player.trashItem, ItemSlot.Context.TrashItem, position);
        Main.inventoryScale = oldScale;
    }

    private static void DrawLoaderSlot(Item[] items, int context, int slot, int x, int y, Player player, int forcedSize = -1)
    {
        float old = Main.inventoryScale;
        int size = forcedSize > 0 ? forcedSize : Main.screenWidth < 1200 ? 36 : 40;
        Main.inventoryScale = size / (float)TextureAssets.InventoryBack.Width();

        int w = (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale);
        int h = (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale);

        bool hover = new Rectangle(x, y, w, h).Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;

        ref Item it = ref items[slot];

        // Normal access OR slot is empty
        if (!it.IsAir && hover)
        {
            UICommon.TooltipMouseText("");
            Main.LocalPlayer.mouseInterface = true;
            ItemSlot.OverrideHover(items, context, slot);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
            }
            if (Main.mouseRight && Main.mouseRightRelease && player == Main.LocalPlayer)
            {
                ItemSlot.RightClick(items, context, slot);
                Main.mouseRightRelease = false;
            }

            ItemSlot.MouseHover(items, context, slot);
        }

        ItemSlot.Draw(Main.spriteBatch, ref it, context, new Vector2(x, y));
        Main.inventoryScale = old;
    }

    public static void DrawEquipment(SpriteBatch sb, Player player, Rectangle viewport)
    {
        int size = Main.screenWidth < 1200 ? 36 : 40;
        int pad = 4;
        int rowStep = size + pad;
        int rightMargin = 30;
        int bottomMargin = 120;

        int dyeRows = Math.Max(0, player.dye.Length - 3);
        int equipRows = Math.Max(0, player.armor.Length - 3);
        int vanityRows = Math.Max(0, player.armor.Length - 13);
        int accessoryRows = Math.Min(7, Math.Min(dyeRows, Math.Min(equipRows, vanityRows)));
        int rows = 3 + accessoryRows;

        int width = rowStep * 3 - pad;
        int height = rows * rowStep - pad;

        Vector2 topLeft = new(viewport.Right - width - rightMargin, viewport.Bottom - height - bottomMargin);

        float bottomAccessoryY = topLeft.Y + (rows - 1) * rowStep;
        DrawDefenseCounter(player, topLeft.X+40, bottomAccessoryY);
        DrawAccessories(sb, topLeft, player, viewport);
    }

    private static void DrawDefenseCounter(Player player, float inventoryX, float inventoryY)
    {
        float oldScale = Main.inventoryScale;
        Main.inventoryScale = Main.screenWidth < 1200 ? 36f / TextureAssets.InventoryBack.Width() : 40f / TextureAssets.InventoryBack.Width();

        Vector2 position = new Vector2(inventoryX - 28, inventoryY + TextureAssets.InventoryBack.Height() * Main.inventoryScale * 0.5f);
        Texture2D texture = TextureAssets.Extra[ExtrasID.DefenseShield].Value;

        Main.spriteBatch.Draw(texture, position, null, Color.White, 0f, texture.Size() / 2f, 1, SpriteEffects.None, 0f);

        string defenseText = player.statDefense.ToString();
        const float defenseTextScale = 1f;

        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(defenseText);
        Vector2 textOrigin = textSize * 0.5f;
        Vector2 textPosition = position + new Vector2(0f, 1f);

        ChatManager.DrawColorCodedStringWithShadow(
            Main.spriteBatch,
            FontAssets.MouseText.Value,
            defenseText,
            textPosition,
            Color.White,
            0f,
            textOrigin,
            new Vector2(defenseTextScale));

        Rectangle hoverBox = Utils.CenteredRectangle(position, texture.Size() * Main.inventoryScale);

        if (hoverBox.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.hoverItemName = player.statDefense + " " + Lang.inter[10].Value;
        }

        UILinkPointNavigator.SetPosition(1557, position + texture.Size() * Main.inventoryScale / 4f);
        Main.inventoryScale = oldScale;
    }

    private static void DrawAccessories(SpriteBatch sb, Vector2 topLeft, Player player, Rectangle viewport)
    {
        int size = Main.screenWidth < 1200 ? 36 : 40;
        int pad = 4;
        int rowStep = size + pad;

        const float titleScale = 0.5f;

        string title = "Accessories";
        Vector2 titleSize = FontAssets.DeathText.Value.MeasureString(title) * titleScale;
        float twoColumnCenterX = topLeft.X + rowStep + (rowStep * 2 - pad) * 0.5f;
        Vector2 titlePosition = new(twoColumnCenterX - titleSize.X * 0.5f, topLeft.Y - 28f);

        Utils.DrawBorderStringBig(sb, title, titlePosition, Color.White, titleScale);

        // Armor rows (head/body/legs + dyes + vanity)
        for (int r = 0; r < 3; r++)
        {
            int y = (int)topLeft.Y + r * rowStep;

            int x0 = (int)topLeft.X + 0 * rowStep;
            int x1 = (int)topLeft.X + 1 * rowStep;
            int x2 = (int)topLeft.X + 2 * rowStep;

            // draw 3 armor dye slots
            DrawLoaderSlot(player.dye, ItemSlot.Context.EquipDye, r, x0, y, player);

            // draw 3 vanity slots
            DrawLoaderSlot(player.armor, ItemSlot.Context.InWorld, 10 + r, x1, y, player);

            // draw 3 armor slots
            DrawLoaderSlot(player.armor, ItemSlot.Context.InWorld, r, x2, y, player);

            // backup visuals
            float scaleBackup = Main.inventoryScale;
            Main.inventoryScale = size / (float)TextureAssets.InventoryBack.Width();

            //if (PlayerInfoDrawer.HasAccess(Main.LocalPlayer, player))
            //{
                ItemSlot.Draw(sb, player.armor, ItemSlot.Context.EquipArmorVanity, 10 + r, new Vector2(x1, y));
                ItemSlot.Draw(sb, player.armor, ItemSlot.Context.EquipArmor, r, new Vector2(x2, y));
            //}
            //else
            //{
            //    Item[] _ghostArmor = Enumerable.Repeat(new Item(), 30).ToArray();
            //    Item[] _ghostDye = Enumerable.Repeat(new Item(), 10).ToArray();
            //    ItemSlot.Draw(sb, _ghostArmor, ItemSlot.Context.EquipArmorVanity, 10 + r, new Vector2(x1, y));
            //    ItemSlot.Draw(sb, _ghostArmor, ItemSlot.Context.EquipArmor, r, new Vector2(x2, y));
            //}


            Main.inventoryScale = scaleBackup;

            bool SlotHasItem(Item[] arr, int index)
                => index >= 0 && index < arr.Length && !arr[index].IsAir;
        }

        // Accessories rows
        Vector2 accTopLeft = new(topLeft.X, topLeft.Y + 3 * rowStep);

        int dyeRows = Math.Max(0, player.dye.Length - 3);
        int equipRows = Math.Max(0, player.armor.Length - 3);
        int vanityRows = Math.Max(0, player.armor.Length - 13);
        int totalRows = Math.Min(7, Math.Min(dyeRows, Math.Min(equipRows, vanityRows)));

        for (int r = 0; r < totalRows; r++)
        {
            int y = (int)accTopLeft.Y + r * rowStep;
            if (y + size > viewport.Bottom) break;

            int dyeIndex = 3 + r;
            int vanityIndex = 13 + r;
            int equipIndex = 3 + r;

            int x0 = (int)accTopLeft.X + 0 * rowStep;
            int x1 = (int)accTopLeft.X + 1 * rowStep;
            int x2 = (int)accTopLeft.X + 2 * rowStep;

            // draw accessory dye slots
            DrawLoaderSlot(player.dye, ItemSlot.Context.EquipDye, dyeIndex, x0, y, player);

            // draw accessory vanity slots
            DrawLoaderSlot(player.armor, ItemSlot.Context.EquipAccessoryVanity, vanityIndex, x1, y, player);

            // draw accessory slots
            DrawLoaderSlot(player.armor, ItemSlot.Context.EquipAccessory, equipIndex, x2, y, player);
        }
    }

    private static void DrawBuffs(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        const int size = 32;
        const int pad = 8;
        const int perRow = 9;
        const float timeScale = 0.75f;

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Point mouse = Main.MouseScreen.ToPoint();

        bool debugBuffs = false;
        int[] buffTypes = player.buffType;
        int[] buffTimes = player.buffTime;

//#if DEBUG
//        debugBuffs = true;
//        buffTypes = [BuffID.Regeneration, BuffID.Swiftness, BuffID.Ironskin, BuffID.WellFed, BuffID.Shine, BuffID.NightOwl, BuffID.Hunter, BuffID.Spelunker, BuffID.Featherfall, BuffID.Gravitation, BuffID.ObsidianSkin, BuffID.WaterWalking, BuffID.Gills, BuffID.Mining, BuffID.Builder];
//        buffTimes = new int[buffTypes.Length];
//        for (int i = 0; i < buffTimes.Length; i++)
//            buffTimes[i] = 60 * (30 + i * 20);
//#endif

        int n = 0;

        for (int i = 0; i < buffTypes.Length; i++)
        {
            int id = buffTypes[i];
            int buffTime = buffTimes[i];

            if (id <= 0 || (!debugBuffs && !player.HasBuff(id)) || id >= TextureAssets.Buff.Length || TextureAssets.Buff[id]?.Value == null)
                continue;

            int row = n / perRow;
            int col = n % perRow;

            Rectangle iconRect = new(
                (int)(start.X + col * (size + pad)),
                (int)(start.Y + row * (size + pad + 16)),
                size,
                size);

            if (iconRect.Bottom > viewport.Bottom - 10)
                break;

            bool hover = iconRect.Contains(mouse) && !PlayerInput.IgnoreMouseInterface;
            float alpha = hover ? 1f : 0.6f;

            sb.Draw(TextureAssets.Buff[id].Value, iconRect, Color.White * alpha);

            if (buffTime > 2 && !Main.buffNoTimeDisplay[id])
            {
                string timeText = GetBuffTimeText(buffTime);
                Vector2 textSize = font.MeasureString(timeText) * timeScale;
                Vector2 textPos = new(iconRect.Center.X - textSize.X * 0.5f, iconRect.Bottom - 1f);

                Utils.DrawBorderString(sb, timeText, textPos, Color.White * alpha, timeScale);
            }

            if (hover)
            {
                string name = Lang.GetBuffName(id);
                string desc = Lang.GetBuffDescription(id);
                string tooltip = string.IsNullOrEmpty(desc) ? name : name + "\n" + desc;

                Main.instance.MouseText(tooltip);
                Main.LocalPlayer.mouseInterface = true;
            }

            n++;
        }
    }

    private static string GetBuffTimeText(int ticks)
    {
        int seconds = ticks / 60;

        if (seconds >= 60)
            return seconds / 60 + " m";

        return seconds + " s";
    }
}
