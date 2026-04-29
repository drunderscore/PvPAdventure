using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Core.Debug;

internal class DebugInventoryDrawer : ModSystem
{
    public override void Load()
    {
        On_Main.DrawInventory += OnMainDrawInventory;
        On_ChestUI.Draw += OnChestUIDraw;
        On_ChestUI.DrawButtons += OnChestUIDrawButtons;
        //On_ItemSlot.Draw
    }

    private void OnMainDrawInventory(On_Main.orig_DrawInventory orig, Main self)
    {
        //orig(self);
        Recipe.GetThroughDelayedFindRecipes();
        if (Main.ShouldPVPDraw)
        {
            Main.DrawPVPIcons();
        }
        int num = 0;
        int num2 = 0;
        int num3 = Main.screenWidth;
        int num4 = 0;
        int num5 = Main.screenWidth;
        int num6 = 0;
        Vector2 vector = new Vector2(num, num2);
        new Vector2(num3, num4);
        new Vector2(num5, num6);
        Main.DrawBestiaryIcon(num, num2);
        Main.DrawEmoteBubblesButton(num, num2);
        Main.DrawTrashItemSlot(num, num2);
        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[4].Value, new Vector2(40f, 0f) + vector, new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
        Main.inventoryScale = 0.85f;
        if (Main.mouseX > 20 && Main.mouseX < (int)(20f + 560f * Main.inventoryScale) && Main.mouseY > 20 && Main.mouseY < (int)(20f + 280f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
        {
            Main.player[Main.myPlayer].mouseInterface = true;
        }
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int num7 = (int)(20f + (float)(i * 56) * Main.inventoryScale) + num;
                int num8 = (int)(20f + (float)(j * 56) * Main.inventoryScale) + num2;
                int num9 = i + j * 10;
                new Color(100, 100, 100, 100);
                if (Main.mouseX >= num7 && (float)Main.mouseX <= (float)num7 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num8 && (float)Main.mouseY <= (float)num8 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    ItemSlot.OverrideHover(Main.player[Main.myPlayer].inventory, 0, num9);
                    if (Main.player[Main.myPlayer].inventoryChestStack[num9] && (Main.player[Main.myPlayer].inventory[num9].type == 0 || Main.player[Main.myPlayer].inventory[num9].stack == 0))
                    {
                        Main.player[Main.myPlayer].inventoryChestStack[num9] = false;
                    }
                    if (!Main.player[Main.myPlayer].inventoryChestStack[num9])
                    {
                        ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 0, num9);
                        ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 0, num9);
                        if (Main.mouseLeftRelease && Main.mouseLeft)
                        {
                            Recipe.FindRecipes();
                        }
                    }
                    ItemSlot.MouseHover(Main.player[Main.myPlayer].inventory, 0, num9);
                }
                ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].inventory, 0, num9, new Vector2(num7, num8));
            }
        }
        int activeToggles = BuilderToggleLoader.ActiveBuilderToggles();
        bool pushSideToolsUp = activeToggles / 12 != BuilderToggleLoader.BuilderTogglePage || activeToggles % 12 >= 10;
        if (!PlayerInput.UsingGamepad)
        {
            Main.instance.DrawHotbarLockIcon(num, num2, pushSideToolsUp);
        }
        ItemSlot.DrawRadialDpad(Main.spriteBatch, new Vector2(20f) + new Vector2(56f * Main.inventoryScale * 10f, 56f * Main.inventoryScale * 5f) + new Vector2(26f, 70f) + vector);
        if (Main.instance._achievementAdvisor.CanDrawAboveCoins)
        {
            int num10 = (int)(20f + 560f * Main.inventoryScale) + num;
            int num11 = (int)(20f + 0f * Main.inventoryScale) + num2;
            Main.instance._achievementAdvisor.DrawOneAchievement(Main.spriteBatch, new Vector2(num10, num11) + new Vector2(5f), large: true);
        }
        if (Main.mapEnabled)
        {
            bool flag = false;
            int num12 = num3 - 440;
            int num13 = 40 + num4;
            if (Main.screenWidth < 940)
            {
                flag = true;
            }
            if (flag)
            {
                num12 = num5 - 40;
                num13 = num6 - 200;
            }
            int num14 = 0;
            for (int k = 0; k < 4; k++)
            {
                int num15 = 255;
                int num16 = num12 + k * 32 - num14;
                int num17 = num13;
                if (flag)
                {
                    num16 = num12;
                    num17 = num13 + k * 32 - num14;
                }
                int num18 = k;
                num15 = 120;
                if (k > 0 && Main.mapStyle == k - 1)
                {
                    num15 = 200;
                }
                if (Main.mouseX >= num16 && Main.mouseX <= num16 + 32 && Main.mouseY >= num17 && Main.mouseY <= num17 + 30 && !PlayerInput.IgnoreMouseInterface)
                {
                    num15 = 255;
                    num18 += 4;
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        if (k == 0)
                        {
                            Main.playerInventory = false;
                            Main.player[Main.myPlayer].SetTalkNPC(-1);
                            Main.npcChatCornerItem = 0;
                            SoundEngine.PlaySound(10);
                            Main.mapFullscreenScale = 2.5f;
                            Main.mapFullscreen = true;
                            Main.resetMapFull = true;
                        }
                        if (k == 1)
                        {
                            Main.mapStyle = 0;
                            SoundEngine.PlaySound(12);
                        }
                        if (k == 2)
                        {
                            Main.mapStyle = 1;
                            SoundEngine.PlaySound(12);
                        }
                        if (k == 3)
                        {
                            Main.mapStyle = 2;
                            SoundEngine.PlaySound(12);
                        }
                    }
                }
                Main.spriteBatch.Draw(TextureAssets.MapIcon[num18].Value, new Vector2(num16, num17), new Rectangle(0, 0, TextureAssets.MapIcon[num18].Width(), TextureAssets.MapIcon[num18].Height()), new Color(num15, num15, num15, num15), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            }
        }
        if (Main.armorHide)
        {
            Main.armorAlpha -= 0.1f;
            if (Main.armorAlpha < 0f)
            {
                Main.armorAlpha = 0f;
            }
        }
        else
        {
            Main.armorAlpha += 0.025f;
            if (Main.armorAlpha > 1f)
            {
                Main.armorAlpha = 1f;
            }
        }
        new Color((byte)((float)(int)Main.mouseTextColor * Main.armorAlpha), (byte)((float)(int)Main.mouseTextColor * Main.armorAlpha), (byte)((float)(int)Main.mouseTextColor * Main.armorAlpha), (byte)((float)(int)Main.mouseTextColor * Main.armorAlpha));
        Main.armorHide = false;
        int num19 = 8 + Main.player[Main.myPlayer].GetAmountOfExtraAccessorySlotsToShow();
        int num20 = 174 + Main.mH;
        int num21 = 950;
        Main._cannotDrawAccessoriesHorizontally = false;
        if (Main.screenHeight < num21 && num19 >= 10)
        {
            num20 -= (int)(56f * Main.inventoryScale * (float)(num19 - 9));
            Main._cannotDrawAccessoriesHorizontally = true;
        }
        int num22 = Main.DrawPageIcons(num20 - 32);
        if (num22 > -1)
        {
            Main.HoverItem = new Item();
            switch (num22)
            {
                case 1:
                    Main.hoverItemName = Lang.inter[80].Value;
                    break;
                case 2:
                    Main.hoverItemName = Lang.inter[79].Value;
                    break;
                case 3:
                    Main.hoverItemName = (Main.CaptureModeDisabled ? Lang.inter[115].Value : Lang.inter[81].Value);
                    break;
            }
        }
        if (Main.EquipPage == 2)
        {
            Point value = new Point(Main.mouseX, Main.mouseY);
            Rectangle r = new Rectangle(0, 0, (int)((float)TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)((float)TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            Item[] inv = Main.player[Main.myPlayer].miscEquips;
            int num23 = Main.screenWidth - 92;
            int num24 = Main.mH + 174;
            for (int l = 0; l < 2; l++)
            {
                switch (l)
                {
                    case 0:
                        inv = Main.player[Main.myPlayer].miscEquips;
                        break;
                    case 1:
                        inv = Main.player[Main.myPlayer].miscDyes;
                        break;
                }
                r.X = num23 + l * -47;
                for (int m = 0; m < 5; m++)
                {
                    int context = 0;
                    int num25 = -1;
                    bool flag2 = false;
                    switch (m)
                    {
                        case 0:
                            context = 19;
                            num25 = 0;
                            break;
                        case 1:
                            context = 20;
                            num25 = 1;
                            break;
                        case 2:
                            context = 18;
                            flag2 = Main.player[Main.myPlayer].unlockedSuperCart;
                            break;
                        case 3:
                            context = 17;
                            break;
                        case 4:
                            context = 16;
                            break;
                    }
                    if (l == 1)
                    {
                        context = 33;
                        num25 = -1;
                        flag2 = false;
                    }
                    r.Y = num24 + m * 47;
                    bool flag3 = false;
                    Texture2D value2 = TextureAssets.InventoryTickOn.Value;
                    Rectangle r2 = new Rectangle(r.Left + 34, r.Top - 2, value2.Width, value2.Height);
                    int num26 = 0;
                    if (num25 != -1)
                    {
                        if (Main.player[Main.myPlayer].hideMisc[num25])
                        {
                            value2 = TextureAssets.InventoryTickOff.Value;
                        }
                        if (r2.Contains(value) && !PlayerInput.IgnoreMouseInterface)
                        {
                            Main.player[Main.myPlayer].mouseInterface = true;
                            flag3 = true;
                            if (Main.mouseLeft && Main.mouseLeftRelease)
                            {
                                if (num25 == 0)
                                {
                                    Main.player[Main.myPlayer].TogglePet();
                                }
                                if (num25 == 1)
                                {
                                    Main.player[Main.myPlayer].ToggleLight();
                                }
                                Main.mouseLeftRelease = false;
                                SoundEngine.PlaySound(12);
                                if (Main.netMode == 1)
                                {
                                    NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                                }
                            }
                            num26 = ((!Main.player[Main.myPlayer].hideMisc[num25]) ? 1 : 2);
                        }
                    }
                    if (flag2)
                    {
                        value2 = TextureAssets.Extra[255].Value;
                        if (!Main.player[Main.myPlayer].enabledSuperCart)
                        {
                            value2 = TextureAssets.Extra[256].Value;
                        }
                        r2 = new Rectangle(r2.X + r2.Width / 2, r2.Y + r2.Height / 2, r2.Width, r2.Height);
                        r2.Offset(-r2.Width / 2, -r2.Height / 2);
                        if (r2.Contains(value) && !PlayerInput.IgnoreMouseInterface)
                        {
                            Main.player[Main.myPlayer].mouseInterface = true;
                            flag3 = true;
                            if (Main.mouseLeft && Main.mouseLeftRelease)
                            {
                                Main.player[Main.myPlayer].enabledSuperCart = !Main.player[Main.myPlayer].enabledSuperCart;
                                Main.mouseLeftRelease = false;
                                SoundEngine.PlaySound(12);
                                if (Main.netMode == 1)
                                {
                                    NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                                }
                            }
                            num26 = ((!Main.player[Main.myPlayer].enabledSuperCart) ? 1 : 2);
                        }
                    }
                    if (r.Contains(value) && !flag3 && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.player[Main.myPlayer].mouseInterface = true;
                        Main.armorHide = true;
                        ItemSlot.Handle(inv, context, m);
                    }
                    ItemSlot.Draw(Main.spriteBatch, inv, context, m, r.TopLeft());
                    if (num25 != -1)
                    {
                        Main.spriteBatch.Draw(value2, r2.TopLeft(), Color.White * 0.7f);
                        if (num26 > 0)
                        {
                            Main.HoverItem = new Item();
                            Main.hoverItemName = Lang.inter[58 + num26].Value;
                        }
                    }
                    if (flag2)
                    {
                        Main.spriteBatch.Draw(value2, r2.TopLeft(), Color.White);
                        if (num26 > 0)
                        {
                            Main.HoverItem = new Item();
                            Main.hoverItemName = Language.GetTextValue((num26 == 1) ? "GameUI.SuperCartDisabled" : "GameUI.SuperCartEnabled");
                        }
                    }
                }
            }
            num24 += 247;
            num23 += 8;
            int num27 = -1;
            int num28 = 0;
            int num29 = 3;
            int num30 = 260;
            if (Main.screenHeight > 630 + num30 * (Main.mapStyle == 1).ToInt())
            {
                num29++;
            }
            if (Main.screenHeight > 680 + num30 * (Main.mapStyle == 1).ToInt())
            {
                num29++;
            }
            if (Main.screenHeight > 730 + num30 * (Main.mapStyle == 1).ToInt())
            {
                num29++;
            }
            int num31 = 46;
            for (int n = 0; n < Player.maxBuffs; n++)
            {
                if (Main.player[Main.myPlayer].buffType[n] != 0)
                {
                    int num32 = num28 / num29;
                    int num33 = num28 % num29;
                    Point point = new Point(num23 + num32 * -num31, num24 + num33 * num31);
                    num27 = Main.DrawBuffIcon(num27, n, point.X, point.Y);
                    UILinkPointNavigator.SetPosition(9000 + num28, new Vector2(point.X + 30, point.Y + 30));
                    num28++;
                    if (Main.buffAlpha[n] < 0.65f)
                    {
                        Main.buffAlpha[n] = 0.65f;
                    }
                }
            }
            UILinkPointNavigator.Shortcuts.BUFFS_DRAWN = num28;
            UILinkPointNavigator.Shortcuts.BUFFS_PER_COLUMN = num29;
            if (num27 >= 0)
            {
                int num34 = Main.player[Main.myPlayer].buffType[num27];
                if (num34 > 0)
                {
                    string buffName = Lang.GetBuffName(num34);
                    string buffTooltip = Main.GetBuffTooltip(Main.player[Main.myPlayer], num34);
                    if (num34 == 147)
                    {
                        Main.bannerMouseOver = true;
                    }
                    int rare = 0;
                    if (Main.meleeBuff[num34])
                    {
                        rare = -10;
                    }
                    BuffLoader.ModifyBuffText(num34, ref buffName, ref buffTooltip, ref rare);
                    Main.instance.MouseTextHackZoom(buffName, rare, 0, buffTooltip);
                }
            }
        }
        else if (Main.EquipPage == 1)
        {
            Main.instance.DrawNPCHousesInUI();
        }
        else if (Main.EquipPage == 0)
        {
            int num35 = 4;
            if (Main.mouseX > Main.screenWidth - 64 - 28 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 28) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 448f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            float num36 = Main.inventoryScale;
            bool flag4 = false;
            int num37 = num19 - 1;
            bool flag5 = Main.LocalPlayer.CanDemonHeartAccessoryBeShown();
            bool flag6 = Main.LocalPlayer.CanMasterModeAccessoryBeShown();
            if (Main._settingsButtonIsPushedToSide)
            {
                num37--;
            }
            Color color = Main.inventoryBack;
            Color color2 = new Color(80, 80, 80, 80);
            Main.DrawLoadoutButtons(num20, flag5, flag6);
            int num39 = -1;
            for (int num40 = 0; num40 < 3; num40++)
            {
                if ((num40 == 8 && !flag5) || (num40 == 9 && !flag6))
                {
                    continue;
                }
                num39++;
                bool flag7 = Main.LocalPlayer.IsItemSlotUnlockedAndUsable(num40);
                if (!flag7)
                {
                    flag4 = true;
                }
                int num41 = Main.screenWidth - 64 - 28;
                int num42 = (int)((float)num20 + (float)(num39 * 56) * Main.inventoryScale);
                new Color(100, 100, 100, 100);
                int num43 = Main.screenWidth - 58;
                int num44 = (int)((float)(num20 - 2) + (float)(num39 * 56) * Main.inventoryScale);
                int context2 = 8;
                if (num40 > 2)
                {
                    num42 += num35;
                    num44 += num35;
                    context2 = 10;
                }
                Texture2D value3 = TextureAssets.InventoryTickOn.Value;
                if (Main.player[Main.myPlayer].hideVisibleAccessory[num40])
                {
                    value3 = TextureAssets.InventoryTickOff.Value;
                }
                Rectangle rectangle = new Rectangle(num43, num44, value3.Width, value3.Height);
                int num45 = 0;
                if (num40 > 2 && rectangle.Contains(new Point(Main.mouseX, Main.mouseY)) && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Main.player[Main.myPlayer].hideVisibleAccessory[num40] = !Main.player[Main.myPlayer].hideVisibleAccessory[num40];
                        SoundEngine.PlaySound(12);
                        if (Main.netMode == 1)
                        {
                            NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                        }
                    }
                    num45 = ((!Main.player[Main.myPlayer].hideVisibleAccessory[num40]) ? 1 : 2);
                }
                else if (Main.mouseX >= num41 && (float)Main.mouseX <= (float)num41 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num42 && (float)Main.mouseY <= (float)num42 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.armorHide = true;
                    Main.player[Main.myPlayer].mouseInterface = true;
                    ItemSlot.OverrideHover(Main.player[Main.myPlayer].armor, context2, num40);
                    if (flag7 || Main.mouseItem.IsAir)
                    {
                        ItemSlot.LeftClick(Main.player[Main.myPlayer].armor, context2, num40);
                    }
                    ItemSlot.MouseHover(Main.player[Main.myPlayer].armor, context2, num40);
                }
                if (flag4)
                {
                    Main.inventoryBack = color2;
                }
                ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].armor, context2, num40, new Vector2(num41, num42));
                if (num40 > 2)
                {
                    Main.spriteBatch.Draw(value3, new Vector2(num43, num44), Color.White * 0.7f);
                    if (num45 > 0)
                    {
                        Main.HoverItem = new Item();
                        Main.hoverItemName = Lang.inter[58 + num45].Value;
                    }
                }
            }
            Main.inventoryBack = color;
            if (Main.mouseX > Main.screenWidth - 64 - 28 - 47 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 20 - 47) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 168f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            num39 = -1;
            for (int num46 = 10; num46 < 13; num46++)
            {
                if ((num46 == 18 && !flag5) || (num46 == 19 && !flag6))
                {
                    continue;
                }
                num39++;
                bool num47 = Main.LocalPlayer.IsItemSlotUnlockedAndUsable(num46);
                flag4 = !num47;
                bool flag8 = !num47 && !Main.mouseItem.IsAir;
                int num48 = Main.screenWidth - 64 - 28 - 47;
                int num49 = (int)((float)num20 + (float)(num39 * 56) * Main.inventoryScale);
                new Color(100, 100, 100, 100);
                if (num46 > 12)
                {
                    num49 += num35;
                }
                int context3 = 9;
                if (num46 > 12)
                {
                    context3 = 11;
                }
                if (Main.mouseX >= num48 && (float)Main.mouseX <= (float)num48 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num49 && (float)Main.mouseY <= (float)num49 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    Main.armorHide = true;
                    ItemSlot.OverrideHover(Main.player[Main.myPlayer].armor, context3, num46);
                    if (!flag8)
                    {
                        ItemSlot.LeftClick(Main.player[Main.myPlayer].armor, context3, num46);
                        ItemSlot.RightClick(Main.player[Main.myPlayer].armor, context3, num46);
                    }
                    ItemSlot.MouseHover(Main.player[Main.myPlayer].armor, context3, num46);
                }
                if (flag4)
                {
                    Main.inventoryBack = color2;
                }
                ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].armor, context3, num46, new Vector2(num48, num49));
            }
            Main.inventoryBack = color;
            if (Main.mouseX > Main.screenWidth - 64 - 28 - 47 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 20 - 47) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 168f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            num39 = -1;
            for (int num50 = 0; num50 < 3; num50++)
            {
                if ((num50 == 8 && !flag5) || (num50 == 9 && !flag6))
                {
                    continue;
                }
                num39++;
                bool num51 = Main.LocalPlayer.IsItemSlotUnlockedAndUsable(num50);
                flag4 = !num51;
                bool flag9 = !num51 && !Main.mouseItem.IsAir;
                int num52 = Main.screenWidth - 64 - 28 - 47 - 47;
                int num53 = (int)((float)num20 + (float)(num39 * 56) * Main.inventoryScale);
                new Color(100, 100, 100, 100);
                if (num50 > 2)
                {
                    num53 += num35;
                }
                if (Main.mouseX >= num52 && (float)Main.mouseX <= (float)num52 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num53 && (float)Main.mouseY <= (float)num53 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    Main.armorHide = true;
                    ItemSlot.OverrideHover(Main.player[Main.myPlayer].dye, 12, num50);
                    if (!flag9)
                    {
                        if (Main.mouseRightRelease && Main.mouseRight)
                        {
                            ItemSlot.RightClick(Main.player[Main.myPlayer].dye, 12, num50);
                        }
                        ItemSlot.LeftClick(Main.player[Main.myPlayer].dye, 12, num50);
                    }
                    ItemSlot.MouseHover(Main.player[Main.myPlayer].dye, 12, num50);
                }
                if (flag4)
                {
                    Main.inventoryBack = color2;
                }
                ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].dye, 12, num50, new Vector2(num52, num53));
            }
            Main.inventoryBack = color;
            Vector2 defPos = AccessorySlotLoader.DefenseIconPosition;
            Main.DrawDefenseCounter((int)defPos.X, (int)defPos.Y);
            if (!Main.instance._achievementAdvisor.CanDrawAboveCoins)
            {
                Vector2 achievePos = new Vector2(defPos.X - 10f - 47f - 47f - 14f - 14f, defPos.Y - 56f * Main.inventoryScale * 0.5f);
                Main.instance._achievementAdvisor.DrawOneAchievement(Main.spriteBatch, achievePos, large: false);
                UILinkPointNavigator.SetPosition(1570, achievePos + new Vector2(20f) * Main.inventoryScale);
            }
            Main.inventoryBack = color;
            Main.inventoryScale = num36;
        }
        LoaderManager.Get<AccessorySlotLoader>().DrawAccSlots(num20);
        int num54 = (Main.screenHeight - 600) / 2;
        int num55 = (int)((float)Main.screenHeight / 600f * 250f);
        if (Main.screenHeight < 700)
        {
            num54 = (Main.screenHeight - 508) / 2;
            num55 = (int)((float)Main.screenHeight / 600f * 200f);
        }
        else if (Main.screenHeight < 850)
        {
            num55 = (int)((float)Main.screenHeight / 600f * 225f);
        }
        if (Main.craftingHide)
        {
            Main.craftingAlpha -= 0.1f;
            if (Main.craftingAlpha < 0f)
            {
                Main.craftingAlpha = 0f;
            }
        }
        else
        {
            Main.craftingAlpha += 0.025f;
            if (Main.craftingAlpha > 1f)
            {
                Main.craftingAlpha = 1f;
            }
        }
        Color color3 = new Color((byte)((float)(int)Main.mouseTextColor * Main.craftingAlpha), (byte)((float)(int)Main.mouseTextColor * Main.craftingAlpha), (byte)((float)(int)Main.mouseTextColor * Main.craftingAlpha), (byte)((float)(int)Main.mouseTextColor * Main.craftingAlpha));
        Main.craftingHide = false;
        if (Main.InReforgeMenu)
        {
            if (Main.mouseReforge)
            {
                if (Main.reforgeScale < 1f)
                {
                    Main.reforgeScale += 0.02f;
                }
            }
            else if (Main.reforgeScale > 1f)
            {
                Main.reforgeScale -= 0.02f;
            }
            if (Main.player[Main.myPlayer].chest != -1 || Main.npcShop != 0 || Main.player[Main.myPlayer].talkNPC == -1 || Main.InGuideCraftMenu)
            {
                Main.InReforgeMenu = false;
                Main.player[Main.myPlayer].dropItemCheck();
                Recipe.FindRecipes();
            }
            else
            {
                int num56 = 50;
                int num57 = 270;
                string text = Lang.inter[46].Value + ": ";
                if (Main.reforgeItem.type > 0)
                {
                    int num58 = Main.reforgeItem.value;
                    num58 *= Main.reforgeItem.stack;
                    bool canApplyDiscount = true;
                    if (ItemLoader.ReforgePrice(Main.reforgeItem, ref num58, ref canApplyDiscount))
                    {
                        if (canApplyDiscount && Main.LocalPlayer.discountAvailable)
                        {
                            num58 = (int)((double)num58 * 0.8);
                        }
                        num58 = (int)((double)num58 * Main.player[Main.myPlayer].currentShoppingSettings.PriceAdjustment);
                        num58 /= 3;
                    }
                    string text2 = "";
                    int num59 = 0;
                    int num60 = 0;
                    int num61 = 0;
                    int num62 = 0;
                    int num63 = num58;
                    if (num63 < 1)
                    {
                        num63 = 1;
                    }
                    if (num63 >= 1000000)
                    {
                        num59 = num63 / 1000000;
                        num63 -= num59 * 1000000;
                    }
                    if (num63 >= 10000)
                    {
                        num60 = num63 / 10000;
                        num63 -= num60 * 10000;
                    }
                    if (num63 >= 100)
                    {
                        num61 = num63 / 100;
                        num63 -= num61 * 100;
                    }
                    if (num63 >= 1)
                    {
                        num62 = num63;
                    }
                    if (num59 > 0)
                    {
                        text2 = text2 + "[c/" + Colors.AlphaDarken(Colors.CoinPlatinum).Hex3() + ":" + num59 + " " + Lang.inter[15].Value + "] ";
                    }
                    if (num60 > 0)
                    {
                        text2 = text2 + "[c/" + Colors.AlphaDarken(Colors.CoinGold).Hex3() + ":" + num60 + " " + Lang.inter[16].Value + "] ";
                    }
                    if (num61 > 0)
                    {
                        text2 = text2 + "[c/" + Colors.AlphaDarken(Colors.CoinSilver).Hex3() + ":" + num61 + " " + Lang.inter[17].Value + "] ";
                    }
                    if (num62 > 0)
                    {
                        text2 = text2 + "[c/" + Colors.AlphaDarken(Colors.CoinCopper).Hex3() + ":" + num62 + " " + Lang.inter[18].Value + "] ";
                    }
                    ItemSlot.DrawSavings(Main.spriteBatch, num56 + 130, Main.instance.invBottom, horizontal: true);
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text2, new Vector2((float)(num56 + 50) + FontAssets.MouseText.Value.MeasureString(text).X, num57), Color.White, 0f, Vector2.Zero, Vector2.One);
                    int num64 = num56 + 70;
                    int num65 = num57 + 40;
                    bool num66 = Main.mouseX > num64 - 15 && Main.mouseX < num64 + 15 && Main.mouseY > num65 - 15 && Main.mouseY < num65 + 15 && !PlayerInput.IgnoreMouseInterface;
                    Texture2D value4 = TextureAssets.Reforge[0].Value;
                    if (num66)
                    {
                        value4 = TextureAssets.Reforge[1].Value;
                    }
                    Main.spriteBatch.Draw(value4, new Vector2(num64, num65), null, Color.White, 0f, value4.Size() / 2f, Main.reforgeScale, SpriteEffects.None, 0f);
                    UILinkPointNavigator.SetPosition(304, new Vector2(num64, num65) + value4.Size() / 4f);
                    if (num66)
                    {
                        Main.hoverItemName = Lang.inter[19].Value;
                        if (!Main.mouseReforge)
                        {
                            SoundEngine.PlaySound(12);
                        }
                        Main.mouseReforge = true;
                        Main.player[Main.myPlayer].mouseInterface = true;
                        if (Main.mouseLeftRelease && Main.mouseLeft && Main.player[Main.myPlayer].CanAfford(num58) && ItemLoader.CanReforge(Main.reforgeItem))
                        {
                            Main.player[Main.myPlayer].BuyItem(num58);
                            ItemLoader.PreReforge(Main.reforgeItem);
                            Main.reforgeItem.ResetPrefix();
                            Main.reforgeItem.Prefix(-2);
                            Main.reforgeItem.position.X = Main.player[Main.myPlayer].position.X + (float)(Main.player[Main.myPlayer].width / 2) - (float)(Main.reforgeItem.width / 2);
                            Main.reforgeItem.position.Y = Main.player[Main.myPlayer].position.Y + (float)(Main.player[Main.myPlayer].height / 2) - (float)(Main.reforgeItem.height / 2);
                            ItemLoader.PostReforge(Main.reforgeItem);
                            PopupText.NewText(PopupTextContext.ItemReforge, Main.reforgeItem, Main.reforgeItem.stack, noStack: true);
                            SoundEngine.PlaySound(in SoundID.Item37);
                        }
                    }
                    else
                    {
                        Main.mouseReforge = false;
                    }
                }
                else
                {
                    text = Lang.inter[20].Value;
                }
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, new Vector2(num56 + 50, num57), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, Vector2.One);
                if (Main.mouseX >= num56 && (float)Main.mouseX <= (float)num56 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num57 && (float)Main.mouseY <= (float)num57 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    Main.craftingHide = true;
                    ItemSlot.LeftClick(ref Main.reforgeItem, 5);
                    if (Main.mouseLeftRelease && Main.mouseLeft)
                    {
                        Recipe.FindRecipes();
                    }
                    ItemSlot.RightClick(ref Main.reforgeItem, 5);
                    ItemSlot.MouseHover(ref Main.reforgeItem, 5);
                }
                ItemSlot.Draw(Main.spriteBatch, ref Main.reforgeItem, 5, new Vector2(num56, num57));
            }
        }
        else if (Main.InGuideCraftMenu)
        {
            if (Main.player[Main.myPlayer].chest != -1 || Main.npcShop != 0 || Main.player[Main.myPlayer].talkNPC == -1 || Main.InReforgeMenu)
            {
                Main.InGuideCraftMenu = false;
                Main.player[Main.myPlayer].dropItemCheck();
                Recipe.FindRecipes();
            }
            else
            {
                Main.DrawGuideCraftText(num54, color3, out var inventoryX, out var inventoryY);
                new Color(100, 100, 100, 100);
                if (Main.mouseX >= inventoryX && (float)Main.mouseX <= (float)inventoryX + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= inventoryY && (float)Main.mouseY <= (float)inventoryY + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    Main.craftingHide = true;
                    ItemSlot.OverrideHover(ref Main.guideItem, 7);
                    ItemSlot.LeftClick(ref Main.guideItem, 7);
                    if (Main.mouseLeftRelease && Main.mouseLeft)
                    {
                        Recipe.FindRecipes();
                    }
                    ItemSlot.RightClick(ref Main.guideItem, 7);
                    ItemSlot.MouseHover(ref Main.guideItem, 7);
                }
                ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, 7, new Vector2(inventoryX, inventoryY));
            }
        }
        Main.CreativeMenu.Draw(Main.spriteBatch);
        bool flag10 = Main.CreativeMenu.Enabled && !Main.CreativeMenu.Blocked;
        flag10 |= Main.hidePlayerCraftingMenu;
        if (!Main.InReforgeMenu && !Main.LocalPlayer.tileEntityAnchor.InUse && !flag10)
        {
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
            if (Main.numAvailableRecipes > 0)
            {
                Main.spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[25].Value, new Vector2(76f, 414 + num54), color3, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            }
            for (int num67 = 0; num67 < Recipe.maxRecipes; num67++)
            {
                Main.inventoryScale = 100f / (Math.Abs(Main.availableRecipeY[num67]) + 100f);
                if ((double)Main.inventoryScale < 0.75)
                {
                    Main.inventoryScale = 0.75f;
                }
                if (Main.recFastScroll)
                {
                    Main.inventoryScale = 0.75f;
                }
                if (Main.availableRecipeY[num67] < (float)((num67 - Main.focusRecipe) * 65))
                {
                    if (Main.availableRecipeY[num67] == 0f && !Main.recFastScroll)
                    {
                        SoundEngine.PlaySound(12);
                    }
                    Main.availableRecipeY[num67] += 6.5f;
                    if (Main.recFastScroll)
                    {
                        Main.availableRecipeY[num67] += 130000f;
                    }
                    if (Main.availableRecipeY[num67] > (float)((num67 - Main.focusRecipe) * 65))
                    {
                        Main.availableRecipeY[num67] = (num67 - Main.focusRecipe) * 65;
                    }
                }
                else if (Main.availableRecipeY[num67] > (float)((num67 - Main.focusRecipe) * 65))
                {
                    if (Main.availableRecipeY[num67] == 0f && !Main.recFastScroll)
                    {
                        SoundEngine.PlaySound(12);
                    }
                    Main.availableRecipeY[num67] -= 6.5f;
                    if (Main.recFastScroll)
                    {
                        Main.availableRecipeY[num67] -= 130000f;
                    }
                    if (Main.availableRecipeY[num67] < (float)((num67 - Main.focusRecipe) * 65))
                    {
                        Main.availableRecipeY[num67] = (num67 - Main.focusRecipe) * 65;
                    }
                }
                else
                {
                    Main.recFastScroll = false;
                }
                if (num67 >= Main.numAvailableRecipes || Math.Abs(Main.availableRecipeY[num67]) > (float)num55)
                {
                    continue;
                }
                int num68 = (int)(46f - 26f * Main.inventoryScale);
                int num69 = (int)(410f + Main.availableRecipeY[num67] * Main.inventoryScale - 30f * Main.inventoryScale + (float)num54);
                double num70 = Main.inventoryBack.A + 50;
                double num71 = 255.0;
                if (Math.Abs(Main.availableRecipeY[num67]) > (float)num55 - 100f)
                {
                    num70 = (double)(150f * (100f - (Math.Abs(Main.availableRecipeY[num67]) - ((float)num55 - 100f)))) * 0.01;
                    num71 = (double)(255f * (100f - (Math.Abs(Main.availableRecipeY[num67]) - ((float)num55 - 100f)))) * 0.01;
                }
                new Color((byte)num70, (byte)num70, (byte)num70, (byte)num70);
                Color lightColor = new Color((byte)num71, (byte)num71, (byte)num71, (byte)num71);
                if (!Main.LocalPlayer.creativeInterface && Main.mouseX >= num68 && (float)Main.mouseX <= (float)num68 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num69 && (float)Main.mouseY <= (float)num69 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.HoverOverCraftingItemButton(num67);
                }
                if (Main.numAvailableRecipes <= 0)
                {
                    continue;
                }
                num70 -= 50.0;
                if (num70 < 0.0)
                {
                    num70 = 0.0;
                }
                if (num67 == Main.focusRecipe)
                {
                    UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = 0;
                    if (PlayerInput.SettingsForUI.HighlightThingsForMouse)
                    {
                        ItemSlot.DrawGoldBGForCraftingMaterial = true;
                    }
                }
                else
                {
                    UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
                }
                Color color4 = Main.inventoryBack;
                Main.inventoryBack = new Color((byte)num70, (byte)num70, (byte)num70, (byte)num70);
                ItemSlot.Draw(Main.spriteBatch, ref Main.recipe[Main.availableRecipe[num67]].createItem, 22, new Vector2(num68, num69), lightColor);
                Main.inventoryBack = color4;
            }
            if (Main.numAvailableRecipes > 0)
            {
                UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
                UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
                for (int num72 = 0; num72 < Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem.Count; num72++)
                {
                    if (Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[num72].type == 0)
                    {
                        UILinkPointNavigator.Shortcuts.CRAFT_CurrentIngredientsCount = num72 + 1;
                        break;
                    }
                    int num73 = 80 + num72 * 40;
                    int num74 = 380 + num54;
                    double num75 = Main.inventoryBack.A + 50;
                    double num76 = 255.0;
                    Color white = Color.White;
                    Color white2 = Color.White;
                    num75 = (float)(Main.inventoryBack.A + 50) - Math.Abs(Main.availableRecipeY[Main.focusRecipe]) * 2f;
                    num76 = 255f - Math.Abs(Main.availableRecipeY[Main.focusRecipe]) * 2f;
                    if (num75 < 0.0)
                    {
                        num75 = 0.0;
                    }
                    if (num76 < 0.0)
                    {
                        num76 = 0.0;
                    }
                    white.R = (byte)num75;
                    white.G = (byte)num75;
                    white.B = (byte)num75;
                    white.A = (byte)num75;
                    white2.R = (byte)num76;
                    white2.G = (byte)num76;
                    white2.B = (byte)num76;
                    white2.A = (byte)num76;
                    Main.inventoryScale = 0.6f;
                    if (num75 == 0.0)
                    {
                        break;
                    }
                    if (Main.mouseX >= num73 && (float)Main.mouseX <= (float)num73 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num74 && (float)Main.mouseY <= (float)num74 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.craftingHide = true;
                        Main.player[Main.myPlayer].mouseInterface = true;
                        Main.SetRecipeMaterialDisplayName(num72);
                    }
                    num75 -= 50.0;
                    if (num75 < 0.0)
                    {
                        num75 = 0.0;
                    }
                    UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = 1 + num72;
                    Color color5 = Main.inventoryBack;
                    Main.inventoryBack = new Color((byte)num75, (byte)num75, (byte)num75, (byte)num75);
                    Item tempItem = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[num72];
                    ItemSlot.Draw(Main.spriteBatch, ref tempItem, 22, new Vector2(num73, num74));
                    Main.inventoryBack = color5;
                }
            }
            if (Main.numAvailableRecipes == 0)
            {
                Main.recBigList = false;
            }
            else
            {
                int num77 = 94;
                int num78 = 450 + num54;
                if (Main.InGuideCraftMenu)
                {
                    num78 -= 150;
                }
                bool flag11 = Main.mouseX > num77 - 15 && Main.mouseX < num77 + 15 && Main.mouseY > num78 - 15 && Main.mouseY < num78 + 15 && !PlayerInput.IgnoreMouseInterface;
                int num79 = Main.recBigList.ToInt() * 2 + flag11.ToInt();
                Main.spriteBatch.Draw(TextureAssets.CraftToggle[num79].Value, new Vector2(num77, num78), null, Color.White, 0f, TextureAssets.CraftToggle[num79].Value.Size() / 2f, 1f, SpriteEffects.None, 0f);
                if (flag11)
                {
                    Main.instance.MouseText(Language.GetTextValue("GameUI.CraftingWindow"), 0, 0);
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        if (!Main.recBigList)
                        {
                            Main.recBigList = true;
                            SoundEngine.PlaySound(12);
                        }
                        else
                        {
                            Main.recBigList = false;
                            SoundEngine.PlaySound(12);
                        }
                    }
                }
            }
        }
        Main.hidePlayerCraftingMenu = false;
        if (Main.recBigList && !flag10)
        {
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
            int num80 = 42;
            if ((double)Main.inventoryScale < 0.75)
            {
                Main.inventoryScale = 0.75f;
            }
            int num81 = 340;
            int num82 = 310;
            int num83 = (Main.screenWidth - num82 - 280) / num80;
            int num84 = (Main.screenHeight - num81 - 20) / num80;
            UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow = num83;
            UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn = num84;
            int num85 = 0;
            int num86 = 0;
            int num87 = num82;
            int num88 = num81;
            int num89 = num82 - 20;
            int num90 = num81 + 2;
            if (Main.recStart > Main.numAvailableRecipes - num83 * num84)
            {
                Main.recStart = Main.numAvailableRecipes - num83 * num84;
                if (Main.recStart < 0)
                {
                    Main.recStart = 0;
                }
            }
            if (Main.recStart > 0)
            {
                if (Main.mouseX >= num89 && Main.mouseX <= num89 + TextureAssets.CraftUpButton.Width() && Main.mouseY >= num90 && Main.mouseY <= num90 + TextureAssets.CraftUpButton.Height() && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeftRelease && Main.mouseLeft)
                    {
                        Main.recStart -= num83;
                        if (Main.recStart < 0)
                        {
                            Main.recStart = 0;
                        }
                        SoundEngine.PlaySound(12);
                        Main.mouseLeftRelease = false;
                    }
                }
                Main.spriteBatch.Draw(TextureAssets.CraftUpButton.Value, new Vector2(num89, num90), new Rectangle(0, 0, TextureAssets.CraftUpButton.Width(), TextureAssets.CraftUpButton.Height()), new Color(200, 200, 200, 200), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            }
            if (Main.recStart < Main.numAvailableRecipes - num83 * num84)
            {
                num90 += 20;
                if (Main.mouseX >= num89 && Main.mouseX <= num89 + TextureAssets.CraftUpButton.Width() && Main.mouseY >= num90 && Main.mouseY <= num90 + TextureAssets.CraftUpButton.Height() && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeftRelease && Main.mouseLeft)
                    {
                        Main.recStart += num83;
                        SoundEngine.PlaySound(12);
                        if (Main.recStart > Main.numAvailableRecipes - num83)
                        {
                            Main.recStart = Main.numAvailableRecipes - num83;
                        }
                        Main.mouseLeftRelease = false;
                    }
                }
                Main.spriteBatch.Draw(TextureAssets.CraftDownButton.Value, new Vector2(num89, num90), new Rectangle(0, 0, TextureAssets.CraftUpButton.Width(), TextureAssets.CraftUpButton.Height()), new Color(200, 200, 200, 200), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            }
            for (int num91 = Main.recStart; num91 < Recipe.maxRecipes && num91 < Main.numAvailableRecipes; num91++)
            {
                int num92 = num87;
                int num93 = num88;
                double num94 = Main.inventoryBack.A + 50;
                double num95 = 255.0;
                new Color((byte)num94, (byte)num94, (byte)num94, (byte)num94);
                new Color((byte)num95, (byte)num95, (byte)num95, (byte)num95);
                if (Main.mouseX >= num92 && (float)Main.mouseX <= (float)num92 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num93 && (float)Main.mouseY <= (float)num93 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    if (Main.mouseLeftRelease && Main.mouseLeft)
                    {
                        Main.focusRecipe = num91;
                        Main.recFastScroll = true;
                        Main.recBigList = false;
                        SoundEngine.PlaySound(12);
                        Main.mouseLeftRelease = false;
                        if (PlayerInput.UsingGamepadUI)
                        {
                            UILinkPointNavigator.ChangePage(9);
                            //Main.LockCraftingForMain.instanceCraftClickDuration();
                        }
                    }
                    Main.craftingHide = true;
                    Main.HoverItem = Main.recipe[Main.availableRecipe[num91]].createItem.Clone();
                    ItemSlot.MouseHover(22);
                    Main.hoverItemName = Main.recipe[Main.availableRecipe[num91]].createItem.Name;
                    if (Main.recipe[Main.availableRecipe[num91]].createItem.stack > 1)
                    {
                        Main.hoverItemName = Main.hoverItemName + " (" + Main.recipe[Main.availableRecipe[num91]].createItem.stack + ")";
                    }
                }
                if (Main.numAvailableRecipes > 0)
                {
                    num94 -= 50.0;
                    if (num94 < 0.0)
                    {
                        num94 = 0.0;
                    }
                    UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = num91 - Main.recStart;
                    Color color6 = Main.inventoryBack;
                    Main.inventoryBack = new Color((byte)num94, (byte)num94, (byte)num94, (byte)num94);
                    ItemSlot.Draw(Main.spriteBatch, ref Main.recipe[Main.availableRecipe[num91]].createItem, 22, new Vector2(num92, num93));
                    Main.inventoryBack = color6;
                }
                num87 += num80;
                num85++;
                if (num85 >= num83)
                {
                    num87 = num82;
                    num88 += num80;
                    num85 = 0;
                    num86++;
                    if (num86 >= num84)
                    {
                        break;
                    }
                }
            }
        }
        Vector2 vector2 = FontAssets.MouseText.Value.MeasureString("Coins");
        Vector2 vector3 = FontAssets.MouseText.Value.MeasureString(Lang.inter[26].Value);
        float num96 = vector2.X / vector3.X;
        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[26].Value, new Vector2(496f, 84f + (vector2.Y - vector2.Y * num96) / 2f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default(Vector2), 0.75f * num96, SpriteEffects.None, 0f);
        Main.inventoryScale = 0.6f;
        for (int num97 = 0; num97 < 4; num97++)
        {
            int num98 = 497;
            int num99 = (int)(85f + (float)(num97 * 56) * Main.inventoryScale + 20f);
            int slot = num97 + 50;
            new Color(100, 100, 100, 100);
            if (Main.mouseX >= num98 && (float)Main.mouseX <= (float)num98 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num99 && (float)Main.mouseY <= (float)num99 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
                ItemSlot.OverrideHover(Main.player[Main.myPlayer].inventory, 1, slot);
                ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 1, slot);
                ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 1, slot);
                if (Main.mouseLeftRelease && Main.mouseLeft)
                {
                    Recipe.FindRecipes();
                }
                ItemSlot.MouseHover(Main.player[Main.myPlayer].inventory, 1, slot);
            }
            ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].inventory, 1, slot, new Vector2(num98, num99));
        }
        Vector2 vector4 = FontAssets.MouseText.Value.MeasureString("Ammo");
        Vector2 vector5 = FontAssets.MouseText.Value.MeasureString(Lang.inter[27].Value);
        float num100 = vector4.X / vector5.X;
        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[27].Value, new Vector2(532f, 84f + (vector4.Y - vector4.Y * num100) / 2f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default(Vector2), 0.75f * num100, SpriteEffects.None, 0f);
        Main.inventoryScale = 0.6f;
        for (int num101 = 0; num101 < 4; num101++)
        {
            int num102 = 534;
            int num103 = (int)(85f + (float)(num101 * 56) * Main.inventoryScale + 20f);
            int slot2 = 54 + num101;
            new Color(100, 100, 100, 100);
            if (Main.mouseX >= num102 && (float)Main.mouseX <= (float)num102 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num103 && (float)Main.mouseY <= (float)num103 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
                ItemSlot.OverrideHover(Main.player[Main.myPlayer].inventory, 2, slot2);
                ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 2, slot2);
                ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 2, slot2);
                if (Main.mouseLeftRelease && Main.mouseLeft)
                {
                    Recipe.FindRecipes();
                }
                ItemSlot.MouseHover(Main.player[Main.myPlayer].inventory, 2, slot2);
            }
            ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].inventory, 2, slot2, new Vector2(num102, num103));
        }
        if (Main.npcShop > 0 && (!Main.playerInventory || Main.player[Main.myPlayer].talkNPC == -1))
        {
            Main.SetNPCShopIndex(0);
        }
        if (Main.npcShop > 0 && !Main.recBigList)
        {
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, Lang.inter[28].Value, 504f, Main.instance.invBottom, Color.White * ((float)(int)Main.mouseTextColor / 255f), Color.Black, Vector2.Zero);
            ItemSlot.DrawSavings(Main.spriteBatch, 504f, Main.instance.invBottom);
            Main.inventoryScale = 0.755f;
            if (Main.mouseX > 73 && Main.mouseX < (int)(73f + 560f * Main.inventoryScale) && Main.mouseY > Main.instance.invBottom && Main.mouseY < (int)((float)Main.instance.invBottom + 224f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            for (int num104 = 0; num104 < 10; num104++)
            {
                for (int num105 = 0; num105 < 4; num105++)
                {
                    int num106 = (int)(73f + (float)(num104 * 56) * Main.inventoryScale);
                    int num107 = (int)((float)Main.instance.invBottom + (float)(num105 * 56) * Main.inventoryScale);
                    int slot3 = num104 + num105 * 10;
                    new Color(100, 100, 100, 100);
                    if (Main.mouseX >= num106 && (float)Main.mouseX <= (float)num106 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num107 && (float)Main.mouseY <= (float)num107 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                    {
                        ItemSlot.OverrideHover(Main.instance.shop[Main.npcShop].item, 15, slot3);
                        Main.player[Main.myPlayer].mouseInterface = true;
                        ItemSlot.LeftClick(Main.instance.shop[Main.npcShop].item, 15, slot3);
                        ItemSlot.RightClick(Main.instance.shop[Main.npcShop].item, 15, slot3);
                        ItemSlot.MouseHover(Main.instance.shop[Main.npcShop].item, 15, slot3);
                    }
                    ItemSlot.Draw(Main.spriteBatch, Main.instance.shop[Main.npcShop].item, 15, slot3, new Vector2(num106, num107));
                }
            }
        }
        if (Main.player[Main.myPlayer].chest > -1 && !Main.tileContainer[Main.tile[Main.player[Main.myPlayer].chestX, Main.player[Main.myPlayer].chestY].type])
        {
            Main.player[Main.myPlayer].chest = -1;
            Recipe.FindRecipes();
        }
        int offsetDown = 0;
        UIVirtualKeyboard.ShouldHideText = !PlayerInput.SettingsForUI.ShowGamepadHints;
        if (!PlayerInput.UsingGamepad)
        {
            offsetDown = 9999;
        }
        UIVirtualKeyboard.OffsetDown = offsetDown;
        ChestUI.Draw(Main.spriteBatch);
        Main.LocalPlayer.tileEntityAnchor.GetTileEntity()?.OnInventoryDraw(Main.LocalPlayer, Main.spriteBatch);
        if (Main.player[Main.myPlayer].chest == -1 && Main.npcShop == 0)
        {
            int num108 = 0;
            int num109 = 498;
            int num110 = 244;
            int num111 = TextureAssets.ChestStack[num108].Width();
            int num112 = TextureAssets.ChestStack[num108].Height();
            UILinkPointNavigator.SetPosition(301, new Vector2((float)num109 + (float)num111 * 0.75f, (float)num110 + (float)num112 * 0.75f));
            if (Main.mouseX >= num109 && Main.mouseX <= num109 + num111 && Main.mouseY >= num110 && Main.mouseY <= num110 + num112 && !PlayerInput.IgnoreMouseInterface)
            {
                num108 = 1;
                if (!Main.allChestStackHover)
                {
                    SoundEngine.PlaySound(12);
                    Main.allChestStackHover = true;
                }
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    Main.player[Main.myPlayer].QuickStackAllChests();
                    Recipe.FindRecipes();
                }
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            else if (Main.allChestStackHover)
            {
                SoundEngine.PlaySound(12);
                Main.allChestStackHover = false;
            }
            Main.spriteBatch.Draw(TextureAssets.ChestStack[num108].Value, new Vector2(num109, num110), new Rectangle(0, 0, TextureAssets.ChestStack[num108].Width(), TextureAssets.ChestStack[num108].Height()), Color.White, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            if (!Main.mouseText && num108 == 1)
            {
                Main.instance.MouseText(Language.GetTextValue("GameUI.QuickStackToNearby"), 0, 0);
            }
        }
        if (Main.player[Main.myPlayer].chest != -1 || Main.npcShop != 0)
        {
            return;
        }
        int num113 = 0;
        int num114 = 534;
        int num115 = 244;
        int num116 = 30;
        int num117 = 30;
        UILinkPointNavigator.SetPosition(302, new Vector2((float)num114 + (float)num116 * 0.75f, (float)num115 + (float)num117 * 0.75f));
        bool flag12 = false;
        if (Main.mouseX >= num114 && Main.mouseX <= num114 + num116 && Main.mouseY >= num115 && Main.mouseY <= num115 + num117 && !PlayerInput.IgnoreMouseInterface)
        {
            num113 = 1;
            flag12 = true;
            Main.player[Main.myPlayer].mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                ItemSorting.SortInventory();
                Recipe.FindRecipes();
            }
        }
        if (flag12 != Main.inventorySortMouseOver)
        {
            SoundEngine.PlaySound(12);
            Main.inventorySortMouseOver = flag12;
        }
        Texture2D value5 = TextureAssets.InventorySort[Main.inventorySortMouseOver ? 1u : 0u].Value;
        Main.spriteBatch.Draw(value5, new Vector2(num114, num115), null, Color.White, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
        if (!Main.mouseText && num113 == 1)
        {
            Main.instance.MouseText(Language.GetTextValue("GameUI.SortInventory"), 0, 0);
        }
    }

    private void OnChestUIDraw(On_ChestUI.orig_Draw orig, SpriteBatch spritebatch)
    {
        orig(spritebatch);
    }

    private void OnChestUIDrawButtons(On_ChestUI.orig_DrawButtons orig, SpriteBatch spritebatch)
    {
        orig(spritebatch);
    }
}
