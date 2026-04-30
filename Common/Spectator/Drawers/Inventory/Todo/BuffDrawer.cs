using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

public static class BuffDrawer
{
    private static bool ownedBuffHover;

    private static void DrawBuffs(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        bool ownsBuffHoverThisFrame = false;

        try
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
                    ownsBuffHoverThisFrame = true;
                    Main.LocalPlayer.mouseInterface = true;

                    string name = Lang.GetBuffName(id);
                    string desc = Lang.GetBuffDescription(id);
                    string tooltip = string.IsNullOrEmpty(desc) ? name : name + "\n" + desc;

                    Main.instance.MouseText(tooltip);
                }

                n++;
            }
        }
        finally
        {
            if (ownedBuffHover && !ownsBuffHoverThisFrame)
            {
                Main.HoverItem = new Item();
                Main.hoverItemName = "";
                Main.mouseText = false;
            }

            ownedBuffHover = ownsBuffHoverThisFrame;
        }
        
    }

    private static string GetBuffTimeText(int ticks)
    {
        int seconds = ticks / 60;

        if (seconds >= 60)
            return seconds / 60 + " m";

        return seconds + " s";
    }

    //public static void DrawBuffs(Player player)
    //{
    //    int num = -1;
    //    int num2 = 11;
    //    for (int i = 0; i < Player.maxBuffs; i++)
    //    {
    //        if (player.buffType[i] > 0)
    //        {
    //            _ = player.buffType[i];
    //            int x = 32 + i * 38;
    //            int num3 = 76;
    //            int num4 = i;
    //            while (num4 >= num2)
    //            {
    //                num4 -= num2;
    //                x = 32 + num4 * 38;
    //                num3 += 50;
    //            }
    //            num = DrawBuffIcon(player, i, x, num3);
    //        }
    //        else
    //        {
    //            Main.buffAlpha[i] = 0.4f;
    //        }
    //    }
    //}

    //public static int DrawBuffIcon(Player player, int buffSlotOnPlayer, int x, int y)
    //{
    //    int num = player.buffType[buffSlotOnPlayer];
    //    if (num == 0)
    //    {
    //        return -1;
    //    }
    //    Color color = new Color(Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer]);
    //    Asset<Texture2D> obj = TextureAssets.Buff[num];
    //    Texture2D texture = obj.Value;
    //    Vector2 drawPosition = new Vector2(x, y);
    //    int width = obj.Width();
    //    int height = obj.Height();
    //    Vector2 textPosition = new Vector2(x, y + height);
    //    Rectangle sourceRectangle = new Rectangle(0, 0, width, height);
    //    Rectangle mouseRectangle = new Rectangle(x, y, width, height);
    //    Color drawColor = color;
    //    BuffDrawParams drawParams = new BuffDrawParams(texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor);
    //    bool num2 = !BuffLoader.PreDraw(Main.spriteBatch, num, buffSlotOnPlayer, ref drawParams);
    //    BuffDrawParams buffDrawParams = drawParams;
    //    //(texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor) = (BuffDrawParams)(ref buffDrawParams);
    //    if (!num2)
    //    {
    //        Main.spriteBatch.Draw(texture, drawPosition, sourceRectangle, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
    //    }
    //    BuffLoader.PostDraw(Main.spriteBatch, num, buffSlotOnPlayer, drawParams);
    //    if (Main.TryGetBuffTime(buffSlotOnPlayer, out var buffTimeValue) && buffTimeValue > 2)
    //    {
    //        string text = Lang.LocalizedDuration(new TimeSpan(0, 0, buffTimeValue / 60), abbreviated: true, showAllAvailableUnits: false);
    //        Main.spriteBatch.DrawString(FontAssets.ItemStack.Value, text, textPosition, color, 0f, default(Vector2), 0.8f, SpriteEffects.None, 0f);
    //    }
    //    if (mouseRectangle.Contains(new Point(Main.mouseX, Main.mouseY)))
    //    {
    //        //drawBuffText = buffSlotOnPlayer;
    //        Main.buffAlpha[buffSlotOnPlayer] += 0.1f;
    //        bool flag = Main.mouseRight && Main.mouseRightRelease;
    //        if (PlayerInput.UsingGamepad)
    //        {
    //            flag = Main.mouseLeft && Main.mouseLeftRelease && Main.playerInventory;
    //            if (Main.playerInventory)
    //            {
    //                Main.player[Main.myPlayer].mouseInterface = true;
    //            }
    //        }
    //        else
    //        {
    //            Main.player[Main.myPlayer].mouseInterface = true;
    //        }
    //        if (flag)
    //        {
    //            flag &= BuffLoader.RightClick(num, buffSlotOnPlayer);
    //        }
    //        if (flag)
    //        {
    //            Main.TryRemovingBuff(buffSlotOnPlayer, num);
    //        }
    //    }
    //    else
    //    {
    //        Main.buffAlpha[buffSlotOnPlayer] -= 0.05f;
    //    }
    //    if (Main.buffAlpha[buffSlotOnPlayer] > 1f)
    //    {
    //        Main.buffAlpha[buffSlotOnPlayer] = 1f;
    //    }
    //    else if ((double)Main.buffAlpha[buffSlotOnPlayer] < 0.4)
    //    {
    //        Main.buffAlpha[buffSlotOnPlayer] = 0.4f;
    //    }
    //    if (PlayerInput.UsingGamepad && !Main.playerInventory)
    //    {
    //        //drawBuffText = -1;
    //    }
    //    //return drawBuffText;
    //    return -1;
    //}
}
