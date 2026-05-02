using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.TeammateSpectator.TeammateOverlay.Drawers;

public static class InventoryDrawer
{
    private static readonly Item[] heldItemSlot = [new Item()];

    private static bool ownedHoverLastFrame;
    private static bool ownedHoverThisFrame;
    private static string ownedHoverName = "";
    private static int ownedHoverItemType;
    private static int ownedHoverItemPrefix;
    private static int ownedHoverItemStack;

    public static void DrawInventory(SpriteBatch sb, Vector2 start, Player player, Rectangle viewport)
    {
        float oldInventoryScale = Main.inventoryScale;
        Color oldInventoryBack = Main.inventoryBack;
        bool oldArmorHide = Main.armorHide;

        ownedHoverThisFrame = false;

        try
        {
            Main.inventoryScale = 0.85f;

            string name = player.name + "'s " + Lang.inter[4].Value;
            sb.DrawString(FontAssets.MouseText.Value, name, new Vector2(4f, 0f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            DrawItems(player);
            DrawCoins(player);
            DrawAmmo(player);
            DrawPageIcons(player);
            if (Main.EquipPage == 0)
                DrawAccSlots(player);
            DrawTrash(player);
            DrawArmor(player);
            DrawEquips(player);

            // --- Todos ---
            //DrawCrafting(player);
            //DrawChestUI(player);
            //DrawCursor();
            //DrawBuffs();
            //DrawHousingMenu();
        }
        finally
        {
            Main.inventoryScale = oldInventoryScale;
            Main.inventoryBack = oldInventoryBack;
            Main.armorHide = oldArmorHide;

            FinishHoverFrame();
        }
    }

    public static void ClearOwnedHover()
    {
        if (ownedHoverLastFrame && CurrentHoverMatchesOwned())
            ClearGlobalHover();

        ownedHoverLastFrame = false;
        ownedHoverThisFrame = false;
        ClearOwnedHoverSnapshot();
    }

    private static void HoverItemSlot(Item[] items, int context, int slot)
    {
        OwnHover();
        ItemSlot.OverrideHover(items, context, slot);
        ItemSlot.MouseHover(items, context, slot);
    }

    private static void HoverItemSlot(ref Item item, int context)
    {
        OwnHover();
        ItemSlot.MouseHover(ref item, context);
    }

    private static void SetHoverText(string text)
    {
        OwnHover();
        Main.HoverItem = new Item();
        Main.hoverItemName = text ?? "";
    }

    private static void OwnHover()
    {
        ownedHoverThisFrame = true;
        Main.LocalPlayer.mouseInterface = true;
    }

    private static void FinishHoverFrame()
    {
        if (ownedHoverThisFrame)
            CaptureOwnedHoverSnapshot();
        else if (ownedHoverLastFrame && CurrentHoverMatchesOwned())
            ClearGlobalHover();

        ownedHoverLastFrame = ownedHoverThisFrame;
        ownedHoverThisFrame = false;

        if (!ownedHoverLastFrame)
            ClearOwnedHoverSnapshot();
    }

    private static void ClearGlobalHover()
    {
        Main.HoverItem = new Item();
        Main.hoverItemName = "";
        Main.mouseText = false;
        Main.armorHide = false;
    }

    private static void CaptureOwnedHoverSnapshot()
    {
        ownedHoverName = Main.hoverItemName ?? "";
        ownedHoverItemType = Main.HoverItem?.type ?? 0;
        ownedHoverItemPrefix = Main.HoverItem?.prefix ?? 0;
        ownedHoverItemStack = Main.HoverItem?.stack ?? 0;
    }

    private static void ClearOwnedHoverSnapshot()
    {
        ownedHoverName = "";
        ownedHoverItemType = 0;
        ownedHoverItemPrefix = 0;
        ownedHoverItemStack = 0;
    }

    private static bool CurrentHoverMatchesOwned()
    {
        Item hoverItem = Main.HoverItem;

        return (Main.hoverItemName ?? "") == ownedHoverName &&
            (hoverItem?.type ?? 0) == ownedHoverItemType &&
            (hoverItem?.prefix ?? 0) == ownedHoverItemPrefix &&
            (hoverItem?.stack ?? 0) == ownedHoverItemStack;
    }

    public static void DrawItems(Player player)
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int num7 = (int)(20f + (float)(i * 56) * Main.inventoryScale) + 0;
                int num8 = (int)(20f + (float)(j * 56) * Main.inventoryScale) + 0;
                int num9 = i + j * 10;
                new Color(100, 100, 100, 100);
                if (Main.mouseX >= num7 && (float)Main.mouseX <= (float)num7 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num8 && (float)Main.mouseY <= (float)num8 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                {
                    if (player.inventoryChestStack[num9] && (player.inventory[num9].type == 0 || player.inventory[num9].stack == 0))
                    {
                        player.inventoryChestStack[num9] = false;
                    }
                    if (!player.inventoryChestStack[num9])
                    {
                        //ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 0, num9);
                        //ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 0, num9);
                        //if (Main.mouseLeftRelease && Main.mouseLeft)
                        //{
                        //    Recipe.FindRecipes();
                        //}
                    }
                    HoverItemSlot(player.inventory, 0, num9);
                }
                ItemSlot.Draw(Main.spriteBatch, player.inventory, 0, num9, new Vector2(num7, num8));
            }
        }
    }

    private static void DrawDefenseCounter(Player player)
    {
        Vector2 defPos = AccessorySlotLoader.DefenseIconPosition;

        var inventoryX = defPos.X;
        var inventoryY = defPos.Y;

        Vector2 vector = new Vector2(inventoryX - 10 - 47 - 47 - 14, (float)inventoryY + (float)TextureAssets.InventoryBack.Height() * 0.5f);
        Main.spriteBatch.Draw(TextureAssets.Extra[58].Value, vector, null, Color.White, 0f, TextureAssets.Extra[58].Value.Size() / 2f, Main.inventoryScale, SpriteEffects.None, 0f);
        Vector2 vector2 = FontAssets.MouseText.Value.MeasureString(player.statDefense.ToString());
        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, player.statDefense.ToString(), vector - vector2 * 0.5f * Main.inventoryScale, Color.White, 0f, Vector2.Zero, new Vector2(Main.inventoryScale));
        if (Utils.CenteredRectangle(vector, TextureAssets.Extra[58].Value.Size()).Contains(new Point(Main.mouseX, Main.mouseY)) && !PlayerInput.IgnoreMouseInterface)
        {
            Player.DefenseStat statDefense = player.statDefense;
            string value = statDefense.ToString() + " " + Lang.inter[10].Value;
            if (!string.IsNullOrEmpty(value))
            {
                SetHoverText(value);
            }
        }
        UILinkPointNavigator.SetPosition(1557, vector + TextureAssets.Extra[58].Value.Size() * Main.inventoryScale / 4f);
    }

    public static void DrawPageIcons(Player player)
    {
        int num20 = 174 + Main.mH;
        int num22 = Main.DrawPageIcons(num20 - 32);
        if (num22 > -1)
        {
            string hoverText = "";
            switch (num22)
            {
                case 1:
                    hoverText = Lang.inter[80].Value;
                    break;
                case 2:
                    hoverText = Lang.inter[79].Value;
                    break;
                case 3:
                    hoverText = (Main.CaptureModeDisabled ? Lang.inter[115].Value : Lang.inter[81].Value);
                    break;
            }
            SetHoverText(hoverText);
        }
    }

    public static void DrawArmor(Player player)
    {
        float oldScale = Main.inventoryScale;
        Main.inventoryScale = 0.85f;

        try
        {
            int num19 = 8 + player.GetAmountOfExtraAccessorySlotsToShow();
            int num20 = 174 + Main.mH;


        if (Main.EquipPage == 0)
        {
                int num35 = 4;
                if (Main.mouseX > Main.screenWidth - 64 - 28 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 28) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 448f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                {
                    player.mouseInterface = true;
                }
                float num36 = Main.inventoryScale;
                bool flag4 = false;
                int num37 = num19 - 1;
                bool flag5 = player.CanDemonHeartAccessoryBeShown();
                bool flag6 = player.CanMasterModeAccessoryBeShown();
                if (Main._settingsButtonIsPushedToSide)
                {
                    num37--;
                }
                Color color = Main.inventoryBack;
                Color color2 = new Color(80, 80, 80, 80);
                //Main.DrawLoadoutButtons(num20, flag5, flag6);
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
                    if (player.hideVisibleAccessory[num40])
                    {
                        value3 = TextureAssets.InventoryTickOff.Value;
                    }
                    Rectangle rectangle = new Rectangle(num43, num44, value3.Width, value3.Height);
                    int num45 = 0;
                    if (num40 > 2 && rectangle.Contains(new Point(Main.mouseX, Main.mouseY)) && !PlayerInput.IgnoreMouseInterface)
                    {
                        player.mouseInterface = true;
                        //if (Main.mouseLeft && Main.mouseLeftRelease)
                        //{
                        //    player.hideVisibleAccessory[num40] = !player.hideVisibleAccessory[num40];
                        //    SoundEngine.PlaySound(12);
                        //    if (Main.netMode == 1)
                        //    {
                        //        NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                        //    }
                        //}
                        num45 = ((!player.hideVisibleAccessory[num40]) ? 1 : 2);
                    }
                    else if (Main.mouseX >= num41 && (float)Main.mouseX <= (float)num41 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num42 && (float)Main.mouseY <= (float)num42 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.armorHide = true;
                        if (flag7 || Main.mouseItem.IsAir)
                        {
                            //ItemSlot.LeftClick(Main.player[Main.myPlayer].armor, context2, num40);
                        }
                        HoverItemSlot(player.armor, context2, num40);
                    }
                    if (flag4)
                    {
                        Main.inventoryBack = color2;
                    }
                    ItemSlot.Draw(Main.spriteBatch, player.armor, context2, num40, new Vector2(num41, num42));
                    if (num40 > 2)
                    {
                        Main.spriteBatch.Draw(value3, new Vector2(num43, num44), Color.White * 0.7f);
                        if (num45 > 0)
                        {
                            SetHoverText(Lang.inter[58 + num45].Value);
                        }
                    }
                }
                Main.inventoryBack = color;
                if (Main.mouseX > Main.screenWidth - 64 - 28 - 47 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 20 - 47) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 168f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                {
                    player.mouseInterface = true;
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
                        Main.armorHide = true;
                        if (!flag8)
                        {
                            //ItemSlot.LeftClick(Main.player[Main.myPlayer].armor, context3, num46);
                            //ItemSlot.RightClick(Main.player[Main.myPlayer].armor, context3, num46);
                        }
                        HoverItemSlot(player.armor, context3, num46);
                    }
                    if (flag4)
                    {
                        Main.inventoryBack = color2;
                    }
                    ItemSlot.Draw(Main.spriteBatch, player.armor, context3, num46, new Vector2(num48, num49));
                }
                Main.inventoryBack = color;
                if (Main.mouseX > Main.screenWidth - 64 - 28 - 47 && Main.mouseX < (int)((float)(Main.screenWidth - 64 - 20 - 47) + 56f * Main.inventoryScale) && Main.mouseY > num20 && Main.mouseY < (int)((float)num20 + 168f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                {
                    player.mouseInterface = true;
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
                        Main.armorHide = true;
                        //if (!flag9)
                        //{
                        //    if (Main.mouseRightRelease && Main.mouseRight)
                        //    {
                        //        ItemSlot.RightClick(player.dye, 12, num50);
                        //    }
                        //    ItemSlot.LeftClick(player.dye, 12, num50);
                        //}
                        HoverItemSlot(player.dye, 12, num50);
                    }
                    if (flag4)
                    {
                        Main.inventoryBack = color2;
                    }
                    ItemSlot.Draw(Main.spriteBatch, player.dye, 12, num50, new Vector2(num52, num53));
                }
                Main.inventoryBack = color;
                Vector2 defPos = AccessorySlotLoader.DefenseIconPosition;

                Main.DrawDefenseCounter((int)defPos.X, (int)defPos.Y);

                DrawDefenseCounter(player);

                if (!Main.instance._achievementAdvisor.CanDrawAboveCoins)
                {
                    Vector2 achievePos = new Vector2(defPos.X - 10f - 47f - 47f - 14f - 14f, defPos.Y - 56f * Main.inventoryScale * 0.5f);
                    Main.instance._achievementAdvisor.DrawOneAchievement(Main.spriteBatch, achievePos, large: false);
                    UILinkPointNavigator.SetPosition(1570, achievePos + new Vector2(20f) * Main.inventoryScale);
                }
                Main.inventoryBack = color;
                Main.inventoryScale = num36;
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
        }
    }

    public static void DrawEquips(Player player)
    {
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
                        inv = player.miscEquips;
                        break;
                    case 1:
                        inv = player.miscDyes;
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
                            //if (Main.mouseLeft && Main.mouseLeftRelease)
                            //{
                            //    if (num25 == 0)
                            //    {
                            //        Main.player[Main.myPlayer].TogglePet();
                            //    }
                            //    if (num25 == 1)
                            //    {
                            //        Main.player[Main.myPlayer].ToggleLight();
                            //    }
                            //    Main.mouseLeftRelease = false;
                            //    SoundEngine.PlaySound(12);
                            //    if (Main.netMode == 1)
                            //    {
                            //        NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                            //    }
                            //}
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
                            //if (Main.mouseLeft && Main.mouseLeftRelease)
                            //{
                            //    Main.player[Main.myPlayer].enabledSuperCart = !Main.player[Main.myPlayer].enabledSuperCart;
                            //    Main.mouseLeftRelease = false;
                            //    SoundEngine.PlaySound(12);
                            //    if (Main.netMode == 1)
                            //    {
                            //        NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
                            //    }
                            //}
                            num26 = ((!Main.player[Main.myPlayer].enabledSuperCart) ? 1 : 2);
                        }
                    }
                    if (r.Contains(value) && !flag3 && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.armorHide = true;
                        //ItemSlot.Handle(inv, context, m);
                        HoverItemSlot(inv, context, m);
                    }
                    ItemSlot.Draw(Main.spriteBatch, inv, context, m, r.TopLeft());
                    if (num25 != -1)
                    {
                        Main.spriteBatch.Draw(value2, r2.TopLeft(), Color.White * 0.7f);
                        if (num26 > 0)
                        {
                            SetHoverText(Lang.inter[58 + num26].Value);
                        }
                    }
                    if (flag2)
                    {
                        Main.spriteBatch.Draw(value2, r2.TopLeft(), Color.White);
                        if (num26 > 0)
                        {
                            SetHoverText(Language.GetTextValue((num26 == 1) ? "GameUI.SuperCartDisabled" : "GameUI.SuperCartEnabled"));
                        }
                    }
                }
            }
        }
    }

    public static void DrawAccSlots(Player player)
    {
        if (player?.active != true)
            return;

        float oldScale = Main.inventoryScale;
        Color oldBack = Main.inventoryBack;
        bool oldArmorHide = Main.armorHide;

        try
        {
            Main.inventoryScale = 0.85f;

            int startY = 174 + Main.mH;

            // --- My extra offset, no idea if this right ---
            startY += 140;

            int x = Main.screenWidth - 64 - 28;
            int row = 0;

            for (int slot = 3; slot < player.dye.Length; slot++)
            {
                if (slot == 8 && !player.CanDemonHeartAccessoryBeShown())
                    continue;

                if (slot == 9 && !player.CanMasterModeAccessoryBeShown())
                    continue;

                bool disabled = !player.IsItemSlotUnlockedAndUsable(slot);
                int y = (int)(startY + row * 56f * Main.inventoryScale) + 4;

                Item[][] inventories = [player.armor, player.armor, player.dye];
                int[] contexts = [10, 11, 12];
                int[] slots = [slot, slot + player.dye.Length, slot];
                int[] offsets = [0, -47, -94];

                for (int i = 0; i < inventories.Length; i++)
                {
                    Item[] items = inventories[i];
                    int drawSlot = slots[i];

                    if (items == null || drawSlot < 0 || drawSlot >= items.Length)
                        continue;

                    Main.inventoryBack = disabled ? new Color(80, 80, 80, 80) : oldBack;

                    Vector2 position = new(x + offsets[i], y);
                    Rectangle rect = new(
                        (int)position.X,
                        (int)position.Y,
                        (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale),
                        (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale)
                    );

                    if (rect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.armorHide = true;
                        HoverItemSlot(items, contexts[i], drawSlot);
                    }

                    ItemSlot.Draw(Main.spriteBatch, items, contexts[i], drawSlot, position);
                    Main.inventoryBack = oldBack;
                }

                row++;
            }

            ModAccessorySlotPlayer modPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
            AccessorySlotLoader loader = LoaderManager.Get<AccessorySlotLoader>();

            for (int slot = 0; slot < modPlayer.SlotCount; slot++)
            {
                ModAccessorySlot modSlot = loader.Get(slot, player);

                if (!modSlot.IsEnabled() && !modSlot.IsVisibleWhenNotEnabled())
                    continue;

                bool disabled = !loader.ModdedIsItemSlotUnlockedAndUsable(slot, player);
                int y = (int)(startY + row * 56f * Main.inventoryScale) + 4;

                Item[][] inventories = [modPlayer.exAccessorySlot, modPlayer.exAccessorySlot, modPlayer.exDyesAccessory];
                int[] contexts = [10, 11, 12];
                int[] slots = [slot, slot + modPlayer.SlotCount, slot];
                int[] offsets = [0, -47, -94];

                for (int i = 0; i < inventories.Length; i++)
                {
                    Item[] items = inventories[i];
                    int drawSlot = slots[i];

                    if (items == null || drawSlot < 0 || drawSlot >= items.Length)
                        continue;

                    Main.inventoryBack = disabled ? new Color(80, 80, 80, 80) : oldBack;

                    Vector2 position = new(x + offsets[i], y);
                    Rectangle rect = new(
                        (int)position.X,
                        (int)position.Y,
                        (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale),
                        (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale)
                    );

                    if (rect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.armorHide = true;
                        HoverItemSlot(items, contexts[i], drawSlot);
                    }

                    ItemSlot.Draw(Main.spriteBatch, items, contexts[i], drawSlot, position);
                    Main.inventoryBack = oldBack;
                }

                row++;
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
            Main.inventoryBack = oldBack;
            Main.armorHide = oldArmorHide;
        }
    }

    private static void DrawCoins(Player player)
    {
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
                //ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 1, slot);
                //ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 1, slot);
                //if (Main.mouseLeftRelease && Main.mouseLeft)
                //{
                //    Recipe.FindRecipes();
                //}
                HoverItemSlot(player.inventory, 1, slot);
            }
            ItemSlot.Draw(Main.spriteBatch, player.inventory, 1, slot, new Vector2(num98, num99));
        }
        Main.inventoryScale = 0.85f;
    }

    private static void DrawAmmo(Player player)
    {
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
                //ItemSlot.LeftClick(Main.player[Main.myPlayer].inventory, 2, slot2);
                //ItemSlot.RightClick(Main.player[Main.myPlayer].inventory, 2, slot2);
                //if (Main.mouseLeftRelease && Main.mouseLeft)
                //{
                //    Recipe.FindRecipes();
                //}
                HoverItemSlot(player.inventory, 2, slot2);
            }
            ItemSlot.Draw(Main.spriteBatch, player.inventory, 2, slot2, new Vector2(num102, num103));
        }
        Main.inventoryScale = 0.85f;
    }

    private static void DrawTrash(Player player)
    {
        //Log.Chat(player.name + ", " + player.trashItem);

        Main.inventoryScale = 0.85f;
        int num = 448;
        int num2 = 258;
        if ((player.chest != -1 || Main.npcShop > 0) && !Main.recBigList)
        {
            //num2 += 168;
            Main.inventoryScale = 0.755f;
            num += 5;
        }
        else if ((player.chest == -1 || Main.npcShop == -1) && Main.trashSlotOffset != Point16.Zero)
        {
            num += Main.trashSlotOffset.X;
            num2 += Main.trashSlotOffset.Y;
            Main.inventoryScale = 0.755f;
        }
        DrawHeldItemSlot(player, num - 47, num2);

        new Color(150, 150, 150, 150);
        if (Main.mouseX >= num && (float)Main.mouseX <= (float)num + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num2 && (float)Main.mouseY <= (float)num2 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
        {
            //ItemSlot.LeftClick(ref player.trashItem, 6);
            //if (Main.mouseLeftRelease && Main.mouseLeft)
            //{
            //    Recipe.FindRecipes();
            //}
            HoverItemSlot(ref player.trashItem, 6);
        }
        ItemSlot.Draw(Main.spriteBatch, ref player.trashItem, 6, new Vector2(num, num2));
    }

    private static void DrawHeldItemSlot(Player player, int x, int y)
    {
        heldItemSlot[0] = player.HeldItem?.Clone() ?? new Item();

        if (Main.mouseX >= x && (float)Main.mouseX <= (float)x + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= y && (float)Main.mouseY <= (float)y + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
            HoverItemSlot(heldItemSlot, 13, 0);

        ItemSlot.Draw(Main.spriteBatch, heldItemSlot, 13, 0, new Vector2(x, y));
    }

}
