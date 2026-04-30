using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

// TODO: Netsync...
public static class CursorDrawer
{
    public static void DrawCursor(Vector2 bonus, bool smart=false)
    {
        if (Main.gameMenu && Main.alreadyGrabbingSunOrMoon)
        {
            return;
        }
        if (Main.player[Main.myPlayer].dead || Main.player[Main.myPlayer].mouseInterface)
        {
            Main.ClearSmartInteract();
            Main.TileInteractionLX = (Main.TileInteractionHX = (Main.TileInteractionLY = (Main.TileInteractionHY = -1)));
        }
        Color color = Main.cursorColor;
        if (!Main.gameMenu && Main.LocalPlayer.hasRainbowCursor)
        {
            color = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.25f % 1f, 1f, 0.5f);
        }
        bool flag = UILinkPointNavigator.Available && !PlayerInput.InBuildingMode;
        if (PlayerInput.SettingsForUI.ShowGamepadCursor)
        {
            if ((Main.player[Main.myPlayer].dead && !Main.player[Main.myPlayer].ghost && !Main.gameMenu) || PlayerInput.InvisibleGamepadInMenus)
            {
                return;
            }
            Vector2 t = new Vector2(Main.mouseX, Main.mouseY);
            Vector2 t2 = Vector2.Zero;
            bool flag2 = Main.SmartCursorIsUsed;
            if (flag2)
            {
                PlayerInput.smartSelectPointer.UpdateCenter(Main.ScreenSize.ToVector2() / 2f);
                t2 = PlayerInput.smartSelectPointer.GetPointerPosition();
                if (Vector2.Distance(t2, t) < 1f)
                {
                    flag2 = false;
                }
                else
                {
                    Utils.Swap(ref t, ref t2);
                }
            }
            float num = 1f;
            if (flag2)
            {
                num = 0.3f;
                color = Color.White * Main.GamepadCursorAlpha;
                int num2 = 17;
                int frameX = 0;
                Main.spriteBatch.Draw(TextureAssets.Cursors[num2].Value, t2 + bonus, TextureAssets.Cursors[num2].Frame(1, 1, frameX), color, (float)Math.PI / 2f * Main.GlobalTimeWrappedHourly, TextureAssets.Cursors[num2].Frame(1, 1, frameX).Size() / 2f, Main.cursorScale, SpriteEffects.None, 0f);
            }
            if (smart && !flag)
            {
                color = Color.White * Main.GamepadCursorAlpha * num;
                int num3 = 13;
                int frameX2 = 0;
                Main.spriteBatch.Draw(TextureAssets.Cursors[num3].Value, t + bonus, TextureAssets.Cursors[num3].Frame(2, 1, frameX2), color, 0f, TextureAssets.Cursors[num3].Frame(2, 1, frameX2).Size() / 2f, Main.cursorScale, SpriteEffects.None, 0f);
            }
            else
            {
                color = Color.White;
                int num4 = 15;
                Main.spriteBatch.Draw(TextureAssets.Cursors[num4].Value, new Vector2(Main.mouseX, Main.mouseY) + bonus, null, color, 0f, TextureAssets.Cursors[num4].Value.Size() / 2f, Main.cursorScale, SpriteEffects.None, 0f);
            }
        }
        else
        {
            int num5 = smart.ToInt();
            Main.spriteBatch.Draw(TextureAssets.Cursors[num5].Value, new Vector2(Main.mouseX, Main.mouseY) + bonus + Vector2.One, null, new Color((int)((float)(int)color.R * 0.2f), (int)((float)(int)color.G * 0.2f), (int)((float)(int)color.B * 0.2f), (int)((float)(int)color.A * 0.5f)), 0f, default(Vector2), Main.cursorScale * 1.1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(TextureAssets.Cursors[num5].Value, new Vector2(Main.mouseX, Main.mouseY) + bonus, null, color, 0f, default(Vector2), Main.cursorScale, SpriteEffects.None, 0f);
        }
    }

    public static Vector2 DrawThickCursor(bool smart = false)
    {
        if (Main.ThickMouse)
        {
            bool showGamepadCursor = PlayerInput.SettingsForUI.ShowGamepadCursor;
            if (Main.gameMenu && Main.alreadyGrabbingSunOrMoon)
            {
                return Vector2.Zero;
            }
            if (showGamepadCursor && PlayerInput.InvisibleGamepadInMenus)
            {
                return Vector2.Zero;
            }
            if (showGamepadCursor && Main.player[Main.myPlayer].dead && !Main.player[Main.myPlayer].ghost && !Main.gameMenu)
            {
                return Vector2.Zero;
            }
            bool flag = UILinkPointNavigator.Available && !PlayerInput.InBuildingMode;
            Color mouseBorderColor = Main.MouseBorderColor;
            int num = 11;
            num += smart.ToInt();
            for (int i = 0; i < 4; i++)
            {
                Vector2 vector = Vector2.Zero;
                switch (i)
                {
                    case 0:
                        vector = new Vector2(0f, 1f);
                        break;
                    case 1:
                        vector = new Vector2(1f, 0f);
                        break;
                    case 2:
                        vector = new Vector2(0f, -1f);
                        break;
                    case 3:
                        vector = new Vector2(-1f, 0f);
                        break;
                }
                vector *= 1f;
                vector += Vector2.One * 2f;
                Vector2 origin = new Vector2(2f);
                Rectangle? sourceRectangle = null;
                float scale = Main.cursorScale * 1.1f;
                if (showGamepadCursor)
                {
                    if (smart && !flag)
                    {
                        num = 13;
                        int frameX = 0;
                        vector = Vector2.One;
                        sourceRectangle = TextureAssets.Cursors[num].Frame(2, 1, frameX);
                        origin = TextureAssets.Cursors[num].Frame(2, 1, frameX).Size() / 2f;
                        mouseBorderColor *= Main.GamepadCursorAlpha;
                    }
                    else
                    {
                        num = 15;
                        vector = Vector2.One;
                        origin = TextureAssets.Cursors[num].Value.Size() / 2f;
                    }
                }
                Main.spriteBatch.Draw(TextureAssets.Cursors[num].Value, new Vector2(Main.mouseX, Main.mouseY) + vector, sourceRectangle, mouseBorderColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            return new Vector2(2f);
        }
        return Vector2.Zero;
    }
}
