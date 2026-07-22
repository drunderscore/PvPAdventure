using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.NPCs;

[Autoload(Side = ModSide.Client)]
internal sealed class ShakingChestUI : ModSystem
{
    private const int PageSize = 40;
    private const int ButtonCount = 5;
    private const int ButtonX = 506;
    private const int ButtonY = 40;
    private const int RowSpacing = 26;
    private const int StatusPadding = 8;
    private const float MinimumScale = 0.75f;
    private const float MaximumScale = 1f;
    private const string AssetPath = "PvPAdventure/Assets/Custom/";

    private static readonly float[] ButtonScale =
        [MinimumScale, MinimumScale, MinimumScale, MinimumScale, MinimumScale];
    private static readonly bool[] ButtonHovered = new bool[ButtonCount];

    private static Item[] _items;
    private static NPC _npc;
    private static int _page;
    private static int _pages = 1;
    private static bool _choosingSaveSlot;

    internal static void Open(Item[] items, NPC npc)
    {
        _items = items ?? [];
        _npc = npc;
        _page = 0;
        _pages = Math.Max(1, (_items.Length + PageSize - 1) / PageSize);
        LoadPage();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Shaking Chest",
            Draw,
            InterfaceScaleType.UI));
    }

    private static bool Draw()
    {
        if (!IsOpen())
        {
            _choosingSaveSlot = false;
            ResetButtons();
            return true;
        }

        DrawPager();
        DrawMenu();
        return true;
    }

    private static bool IsOpen() =>
        _items != null && _npc?.active == true && Main.playerInventory && Main.npcShop > 0 &&
        Main.LocalPlayer.talkNPC == _npc.whoAmI;

    private static void LoadPage()
    {
        Item[] shop = Main.instance.shop[Main.npcShop].item;
        for (int i = 0; i < shop.Length; i++)
            shop[i] = new Item();

        int start = _page * PageSize;
        int count = Math.Min(PageSize, _items.Length - start);
        Array.Copy(_items, start, shop, 0, count);
    }

    private static void ChangePage(int direction)
    {
        _page = Math.Clamp(_page + direction, 0, _pages - 1);
        LoadPage();
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private static void DrawPager()
    {
        if (_pages <= 1)
            return;

        float inventoryScale = Main.inventoryScale;
        float centerX = 73f + 280f * inventoryScale;
        float centerY = Main.instance.invBottom + 232f * inventoryScale + 11f;

        DrawArrow(new Vector2(centerX - 58f * inventoryScale, centerY), true, _page > 0);
        DrawArrow(new Vector2(centerX + 58f * inventoryScale, centerY), false, _page < _pages - 1);

        string text = $"{_page + 1}/{_pages}";
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
        Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(centerX, centerY + 4) - size / 2f, Color.White);
    }

    private static void DrawArrow(Vector2 center, bool left, bool enabled)
    {
        Texture2D texture = ArrowTexture(left, enabled);
        bool hovered = Utils.CenteredRectangle(center, texture.Size()).Contains(Main.MouseScreen.ToPoint());

        if (hovered)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(left ? "Previous page" : "Next page");

            if (enabled && Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                ChangePage(left ? -1 : 1);
            }
        }

        Main.spriteBatch.Draw(
            texture,
            center,
            null,
            Color.White,
            0f,
            texture.Size() / 2f,
            hovered && enabled ? 1.1f : 1f,
            SpriteEffects.None,
            0f);
    }

    private static Texture2D ArrowTexture(bool left, bool enabled)
    {
        string color = enabled ? "Green" : "Red";
        string direction = left ? "Left" : "Right";
        return ModContent.Request<Texture2D>($"{AssetPath}Arrow{color}{direction}").Value;
    }

    private static void DrawMenu()
    {
        DrawButton(0, _choosingSaveSlot ? "Cancel Save" : "Save Current Loadout", true, Colors.RarityYellow,
            () => _choosingSaveSlot = !_choosingSaveSlot);

        for (int slot = 0; slot < ShakingChestLoadouts.SlotCount; slot++)
        {
            bool saved = ShakingChestLoadouts.HasSlot(slot);
            int selectedSlot = slot;
            int button = slot + 1;
            string action = _choosingSaveSlot ? $"Save Loadout #{button}" : $"Load Loadout #{button}";
            string status = _choosingSaveSlot && saved ? "Overwrite" : saved ? "Saved" : "Empty";
            Color statusColor = _choosingSaveSlot && saved
                ? Colors.RarityOrange
                : saved ? Colors.RarityGreen : Color.Gray;
            float actionScale = ButtonScale[button];

            DrawButton(button, action, _choosingSaveSlot || saved,
                _choosingSaveSlot ? Colors.RarityYellow : Colors.RarityBlue,
                () => UseSlot(selectedSlot));

            float statusX = ButtonX + FontAssets.MouseText.Value.MeasureString(action).X * actionScale + StatusPadding;
            DrawText(status, statusX, Main.instance.invBottom + ButtonY + button * RowSpacing,
                MinimumScale, statusColor);
        }

        DrawButton(4, "Refund Purchases", true, Colors.RarityRed, () =>
        {
            _choosingSaveSlot = false;
            ShakingChestNPC.RefundPlayer(Main.LocalPlayer);
        });
    }

    private static void UseSlot(int slot)
    {
        if (_choosingSaveSlot)
        {
            bool saved = ShakingChestLoadouts.Save(slot, Main.LocalPlayer);
            Main.NewText(
                saved ? $"Saved current loadout to slot #{slot + 1}." : $"Could not save loadout #{slot + 1}.",
                saved ? Color.LightGreen : Color.OrangeRed);
            _choosingSaveSlot = false;
        }
        else if (ShakingChestLoadouts.Apply(slot, Main.LocalPlayer))
        {
            Main.NewText($"Loaded starting loadout #{slot + 1}.", Color.LightGreen);
        }
    }

    private static void DrawButton(int id, string text, bool enabled, Color enabledColor, Action action)
    {
        int y = Main.instance.invBottom + ButtonY + id * RowSpacing;
        float scale = ButtonScale[id];
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
        int centerX = ButtonX + (int)(size.X * scale / 2f);
        bool hovered = enabled && Utils.FloatIntersect(
            Main.mouseX, Main.mouseY, 0f, 0f,
            centerX - size.X / 2f - (ButtonHovered[id] ? 10f : 0f),
            y - 12f,
            size.X + (ButtonHovered[id] ? 16f : 0f),
            24f);

        Color color = hovered ? Main.OurFavoriteColor : enabled ? enabledColor : Color.Gray;
        DrawText(text, centerX, y, scale, color, centered: true);
        UpdateHover(id, hovered);

        if (hovered && !PlayerInput.IgnoreMouseInterface)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                action();
            }
        }
    }

    private static void UpdateHover(int id, bool hovered)
    {
        if (hovered && !ButtonHovered[id])
            SoundEngine.PlaySound(SoundID.MenuTick);

        ButtonHovered[id] = hovered;
        ButtonScale[id] = Math.Clamp(
            ButtonScale[id] + (hovered ? 0.05f : -0.05f),
            MinimumScale,
            MaximumScale);
    }

    private static void ResetButtons()
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            ButtonScale[i] = MinimumScale;
            ButtonHovered[i] = false;
        }
    }

    private static void DrawText(string text, float x, float y, float scale, Color color, bool centered = false)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
        Vector2 position = centered ? new Vector2(x, y) : new Vector2(x + size.X * scale / 2f, y);
        ChatManager.DrawColorCodedStringWithShadow(
            Main.spriteBatch,
            FontAssets.MouseText.Value,
            text,
            position,
            color,
            0f,
            size / 2f,
            new Vector2(scale),
            -1f,
            1.5f);
    }

    public override void OnWorldUnload()
    {
        _items = null;
        _npc = null;
        _page = 0;
        _pages = 1;
        _choosingSaveSlot = false;
    }
}
