using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.Tabs.NPCs;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.UI;

internal static class StatDrawer
{
    #region Shared draw helpers
    public static void DrawBack(SpriteBatch spriteBatch, Rectangle area)
    {
        Color color = Color.White * 0.7f;

        Asset<Texture2D> texture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
        spriteBatch.Draw(texture.Value, new Vector2(area.X, area.Y), new Rectangle(0, 0, 8, texture.Height()), color);
        spriteBatch.Draw(texture.Value, new Vector2(area.X + 8f, area.Y), new Rectangle(8, 0, 8, texture.Height()), color, 0f, Vector2.Zero, new Vector2((area.Width - 16f) / 8f, 1f), SpriteEffects.None, 0f);
        spriteBatch.Draw(texture.Value, new Vector2(area.Right - 8f, area.Y), new Rectangle(16, 0, 8, texture.Height()), color);
    }

    public static string Truncate(DynamicSpriteFont font, string text, float maxWidth, float scale)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (font.MeasureString(text).X * scale <= maxWidth)
            return text;

        const string ellipsis = "..";

        for (int i = text.Length - 1; i >= 0; i--)
        {
            string candidate = text[..i] + ellipsis;

            if (font.MeasureString(candidate).X * scale <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    public static int GetResponsiveColumns(int width)
    {
        return Math.Clamp(width / 112, 1, 5);
    }

    private static int GetListColumns(int width, int statCount, int rows)
    {
        int columnsByWidth = GetResponsiveColumns(width);
        int columnsByContent = Math.Max(1, (int)Math.Ceiling(statCount / (float)Math.Max(1, rows)));

        return Math.Min(columnsByWidth, columnsByContent);
    }

    public static int GetGridColumns(Rectangle area)
    {
        return GetResponsiveColumns(area.Width);
    }

    private static int GetVisibleListRows(int height, int statSpacing)
    {
        return Math.Max(1, (height + statSpacing) / (FixedListStatHeight + statSpacing));
    }

    private static void DrawStat(SpriteBatch spriteBatch, Rectangle area, Texture2D texture, Rectangle? frame, string text)
    {
        DrawBack(spriteBatch, area);

        Rectangle iconArea = new(area.X + 6, area.Y + 5, 16, 16);
        Rectangle source = frame ?? texture.Bounds;

        if (source.Width > 0 && source.Height > 0)
        {
            float scale = Math.Min(iconArea.Width / (float)source.Width, iconArea.Height / (float)source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));

            spriteBatch.Draw(texture, new Rectangle(iconArea.X, iconArea.Y + (iconArea.Height - height) / 2, width, height), source, Color.White);
        }

        Rectangle textArea = new(area.X + 26, area.Y + 4, area.Width - 30, area.Height - 8);
        Utils.DrawBorderString(spriteBatch, Truncate(FontAssets.MouseText.Value, text, textArea.Width, 0.8f), new Vector2(textArea.X, textArea.Y), Color.White, 0.8f);
    }

    private static void DrawTextureIcon(SpriteBatch spriteBatch, Texture2D texture, Rectangle iconBox, int iconSize, Color color)
    {
        if (texture is null)
            return;

        float scale = Math.Min(iconSize / (float)texture.Width, iconSize / (float)texture.Height);
        spriteBatch.Draw(texture, iconBox.Center.ToVector2(), null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }
    #endregion

    #region Player stats
    private const int FixedListStatHeight = 27;
    public static string DrawPlayerListStats(SpriteBatch spriteBatch, Rectangle area, PlayerStatSnapshot[] stats)
    {
        const int statSpacing = 4;

        int rows = GetVisibleListRows(area.Height, statSpacing);
        int columns = GetListColumns(area.Width, stats.Length, rows);

        return DrawPlayerStatGrid(spriteBatch, area, stats, columns, rows, FixedListStatHeight, statSpacing);
    }

    public static void DrawPlayerStat(SpriteBatch spriteBatch, Rectangle area, in PlayerStatSnapshot stat)
    {
        DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text);
    }

    public static string DrawPlayerStatGrid(SpriteBatch spriteBatch, Rectangle area, PlayerStatSnapshot[] stats, int columns, int rows, int statHeight, int statSpacing)
    {
        if (rows <= 0 || columns <= 0 || stats.Length == 0)
            return null;

        int panelWidth = (area.Width - statSpacing * (columns - 1)) / columns;
        int count = Math.Min(stats.Length, columns * rows);
        Point mouse = Main.MouseScreen.ToPoint();
        string hovered = null;

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle panel = new(area.X + column * (panelWidth + statSpacing), area.Y + row * (statHeight + statSpacing), panelWidth, statHeight);

            DrawPlayerStat(spriteBatch, panel, stats[i]);

            if (panel.Contains(mouse))
                hovered = stats[i].HoverText;
        }

        return hovered;
    }
    #endregion

    #region NPC stats
    public static string DrawNPCListStats(SpriteBatch spriteBatch, Rectangle area, NPCStatSnapshot[] stats)
    {
        const int statSpacing = 4;

        int rows = GetVisibleListRows(area.Height, statSpacing);
        int columns = GetListColumns(area.Width, stats.Length, rows);

        return DrawNPCStatGrid(spriteBatch, area, stats, columns, rows, FixedListStatHeight, statSpacing);
    }

    public static void DrawNPCStat(SpriteBatch spriteBatch, Rectangle area, in NPCStatSnapshot stat)
    {
        DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text);
    }

    public static string DrawNPCStatGrid(SpriteBatch spriteBatch, Rectangle area, NPCStatSnapshot[] stats, int columns, int rows, int statHeight, int statSpacing)
    {
        if (rows <= 0 || columns <= 0 || stats.Length == 0)
            return null;

        int panelWidth = (area.Width - statSpacing * (columns - 1)) / columns;
        int count = Math.Min(stats.Length, columns * rows);
        Point mouse = Main.MouseScreen.ToPoint();
        string hovered = null;

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle panel = new(area.X + column * (panelWidth + statSpacing), area.Y + row * (statHeight + statSpacing), panelWidth, statHeight);

            DrawNPCStat(spriteBatch, panel, stats[i]);

            if (panel.Contains(mouse))
                hovered = stats[i].HoverText;
        }

        return hovered;
    }
    #endregion

    #region World stat panels
    public static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, int itemId, string text, string hoverText, int iconSize = 25, Color? textColor = null)
    {
        DrawWorldStatPanel(spriteBatch, area, text, hoverText, textColor ?? Color.White, iconBox =>
        {
            if (itemId <= 0)
            {
                DrawTextureIcon(spriteBatch, Ass.Icon_World.Value, iconBox, iconSize + 2, Color.White * 0.75f);
                return;
            }

            Item item = new(itemId);
            ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, spriteBatch, iconBox.Center.ToVector2(), 0.9f, iconSize, Color.White);
        });
    }

    public static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, Texture2D icon, string text, string hoverText, int iconSize = 30, Color? textColor = null)
    {
        DrawWorldStatPanel(spriteBatch, area, text, hoverText, textColor ?? Color.White, iconBox => DrawTextureIcon(spriteBatch, icon, iconBox, iconSize, Color.White));
    }

    private static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, string text, string hoverText, Color textColor, Action<Rectangle> drawIcon)
    {
        const float textScale = 0.78f;

        DrawBack(spriteBatch, area);

        Rectangle iconBox = new(area.X + 3, area.Y + 0, area.Height - 2, area.Height - 2);
        Rectangle textArea = new(iconBox.Right + 6, area.Y + 6, area.Width - iconBox.Width - 13, area.Height - 8);

        drawIcon(iconBox);

        string displayText = Truncate(FontAssets.MouseText.Value, text, textArea.Width, textScale);
        Utils.DrawBorderString(spriteBatch, displayText, new Vector2(textArea.X, textArea.Y), textColor, textScale);

        if (area.Contains(Main.MouseScreen.ToPoint()) && !string.IsNullOrWhiteSpace(hoverText))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(hoverText);
        }
    }
    #endregion
}