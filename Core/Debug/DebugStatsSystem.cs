using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal sealed class DebugStatsSystem : ModSystem
{
    // ── Visibility ────────────────────────────────────────────────────
    internal static bool IsVisible { get; private set; }
    internal static void Toggle() => IsVisible = !IsVisible;
    internal static void SetVisible(bool value) => IsVisible = value;

    // ── Per-group enabled state ───────────────────────────────────────
    private static readonly Dictionary<string, bool> _groupEnabled = [];

    private static bool IsGroupEnabled(string header)
        => !_groupEnabled.TryGetValue(header, out bool v) || v;

    private static void ToggleGroup(string header)
        => _groupEnabled[header] = !IsGroupEnabled(header);

    // ── Text draw queue ───────────────────────────────────────────────
    private static readonly List<(string text, Vector2 pos, Color color, float scale)> _texts = [];

    private static void QueueText(string text, Vector2 pos, Color color, float scale = 0.8f)
        => _texts.Add((text, pos, color, scale));
    // Fishing panel
    private static UserInterface fishingUI;
    private static UIState fishingState;
    private static DebugFishingCatchPanel fishingPanel;

    // ── Panel textures ────────────────────────────────────────────────
    private static Texture2D PanelBg => Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;
    private static Texture2D PanelBorder => Main.Assets.Request<Texture2D>("Images/UI/PanelBorder").Value;

    // ── Shared layout origin ──────────────────────────────────────────
    private static int OriginX => Main.screenWidth < 1200 ? Main.screenWidth - 400 : 1100;

    // ── Public entry points ───────────────────────────────────────────
    public override void OnWorldLoad()
    {
        fishingUI = new();
        fishingState = new();
        fishingPanel = new DebugFishingCatchPanel();
        fishingState.Append(fishingPanel);
    }
    internal static void DrawButtons()
    {
        Texture2D back = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel").Value;
        Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder").Value;

        const int spacing = 6;
        const int startY = 6;
        const float labelScale = 0.68f;
        int startX = OriginX;

        int i = 0;
        foreach (DebugStatGroup group in DebugStats.AllGroups())
        {
            string shortName = group.Header.Length >= 2 ? group.Header[..2].ToUpper() : $"S{i}";
            Rectangle rect = new(startX + i * (back.Width + spacing), startY, back.Width, back.Height);

            bool hovered = rect.Contains(Main.MouseScreen.ToPoint());
            bool selected = IsGroupEnabled(group.Header);

            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(group.Header);
            }

            if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
            {
                ToggleGroup(group.Header);
                Main.mouseLeftRelease = false;
            }

            Color fill = selected ? new Color(70, 145, 90) : new Color(145, 70, 70);
            Color borderCol = hovered ? Color.Yellow : selected ? Color.LimeGreen : Color.Black;
            float opacity = selected || hovered ? 1f : 0.82f;
            Vector2 center = rect.Center.ToVector2();

            Main.spriteBatch.Draw(back, center, null, fill * opacity, 0f, back.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(border, center, null, borderCol, 0f, border.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(shortName) * labelScale;
            QueueText(shortName, center - labelSize * 0.5f, Color.White, labelScale);

            i++;
        }
    }

    internal static void DrawStats()
    {
        Vector2 origin = new(OriginX, 46);
        float nextY = origin.Y;

        foreach (DebugStatGroup group in DebugStats.AllGroups())
        {
            if (!IsGroupEnabled(group.Header))
                continue;

            if (group.Header == DebugFishingCatchPanel.GroupHeader)
            {
                DrawFishingPanel(new Vector2(origin.X, nextY));
                nextY += DebugFishingCatchPanel.PanelHeight + 14f;
                continue;
            }

            string[] rows = group.BuildRows();
            DrawGroup(group.Header, group.Color, rows, new Vector2(origin.X, nextY));
            nextY += GroupHeight(rows.Length) + 14f;
        }
    }
    private static void DrawFishingPanel(Vector2 origin)
    {
        if (fishingPanel == null)
            return;

        fishingUI?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
    }

    internal static void Flush(SpriteBatch sb)
    {
        foreach ((string text, Vector2 pos, Color color, float scale) in _texts)
            Utils.DrawBorderString(sb, text, pos, color, scale);

        _texts.Clear();
    }

    // ── Private helpers ───────────────────────────────────────────────
    private static void DrawGroup(string header, Color headerColor, string[] rows, Vector2 origin)
    {
        const float headerScale = 1.0f;
        const float rowScale = 0.8f;
        const float headerGap = 22f;
        const float rowStep = 15f;
        const float columnGap = 8f;
        const float padding = 8f;
        const int slice = 10;

        // Measure content so the panel wraps it tightly
        float labelW = MaxLabelWidth(rows, rowScale);
        float valueW = MaxValueWidth(rows, rowScale);
        float headerW = FontAssets.MouseText.Value.MeasureString(header).X * headerScale;
        float contentW = Math.Max(headerW, labelW + (valueW > 0f ? columnGap + valueW : 0f));
        float contentH = headerGap + rows.Length * rowStep;

        int px = (int)(origin.X - padding);
        int py = (int)(origin.Y - padding);
        int pw = (int)(contentW + padding * 2f);
        int ph = (int)(contentH + padding * 2f);

        // Vanilla UIPanel: background then border, same slice size
        Utils.DrawSplicedPanel(Main.spriteBatch, PanelBg, px, py, pw, ph, slice, slice, slice, slice, new Color(44, 57, 105, 210));
        Utils.DrawSplicedPanel(Main.spriteBatch, PanelBorder, px, py, pw, ph, slice, slice, slice, slice, Color.Black);

        // Text on top
        QueueText(header, origin, headerColor, headerScale);

        float valueX = origin.X + labelW + columnGap;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            int sep = row.IndexOf(':');
            Vector2 rowY = origin + new Vector2(0f, headerGap + i * rowStep);

            if (sep < 0)
            {
                QueueText(row, rowY, Color.White, rowScale);
                continue;
            }

            string label = row[..(sep + 1)];
            string value = sep < row.Length - 1 ? row[(sep + 1)..].TrimStart() : string.Empty;

            QueueText(label, rowY, Color.LightGray, rowScale);
            QueueText(value, new Vector2(valueX, rowY.Y), Color.White, rowScale);
        }
    }

    private static float MaxLabelWidth(string[] rows, float scale)
    {
        float w = 0f;
        foreach (string row in rows)
        {
            int sep = row.IndexOf(':');
            if (sep < 0) continue;
            w = Math.Max(w, FontAssets.MouseText.Value.MeasureString(row[..(sep + 1)]).X * scale);
        }
        return w;
    }

    private static float MaxValueWidth(string[] rows, float scale)
    {
        float w = 0f;
        foreach (string row in rows)
        {
            int sep = row.IndexOf(':');
            if (sep < 0)
            {
                w = Math.Max(w, FontAssets.MouseText.Value.MeasureString(row).X * scale);
                continue;
            }
            if (sep >= row.Length - 1) continue;
            w = Math.Max(w, FontAssets.MouseText.Value.MeasureString(row[(sep + 1)..].TrimStart()).X * scale);
        }
        return w;
    }

    private static float GroupHeight(int rowCount)
    {
        const float headerGap = 22f;
        const float rowStep = 15f;
        return headerGap + rowCount * rowStep;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (KeyboardHelper.Pressed(Keys.F6))
        {
            Toggle();
            SyncBuilderToggle();
        }

        bool showFishingPanel = IsVisible && IsGroupEnabled(DebugFishingCatchPanel.GroupHeader);
        fishingUI?.SetState(showFishingPanel ? fishingState : null);

        if (showFishingPanel)
            LayoutFishingPanel();

        fishingUI?.Update(gameTime);
    }
    private static Vector2 FishingPanelPosition()
    {
        Vector2 origin = new(OriginX, 46);
        float nextY = origin.Y;

        foreach (DebugStatGroup group in DebugStats.AllGroups())
        {
            if (!IsGroupEnabled(group.Header))
                continue;

            if (group.Header == DebugFishingCatchPanel.GroupHeader)
                return new Vector2(origin.X - 8f, nextY - 8f);

            string[] rows = group.BuildRows();
            nextY += GroupHeight(rows.Length) + 14f;
        }

        return new Vector2(OriginX - 8f, 46f - 8f);
    }

    private static void LayoutFishingPanel()
    {
        if (fishingPanel == null)
            return;

        Vector2 position = FishingPanelPosition();
        fishingPanel.Left.Set(position.X, 0f);
        fishingPanel.Top.Set(position.Y, 0f);
        fishingPanel.Width.Set(DebugFishingCatchPanel.PanelWidth, 0f);
        fishingPanel.Height.Set(DebugFishingCatchPanel.PanelHeight, 0f);
        fishingPanel.Recalculate();
        fishingState?.Recalculate();
    }
    public static void RefreshFishingPanelLayout()
    {
        LayoutFishingPanel();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!IsVisible)
            return;

        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (index < 0) return;

        layers.Insert(index, new LegacyGameInterfaceLayer("PvPAdventure: Debug Stats", () =>
        {
            DrawButtons();
            DrawStats();
            Flush(Main.spriteBatch);
            return true;
        }, InterfaceScaleType.UI));
    }

    private static void SyncBuilderToggle()
    {
        DebugStatsBuilderToggle toggle = ModContent.GetInstance<DebugStatsBuilderToggle>();
        if (toggle != null)
            toggle.CurrentState = IsVisible ? 0 : 1;
    }
}

/// <summary>
/// Hotbar builder toggle. Mirrors F6 visibility state.
/// Uses InventoryTickOn/Off textures — no custom asset required.
/// </summary>
public class DebugStatsBuilderToggle : BuilderToggle
{
    public override string Texture => "PvPAdventure/Assets/Custom/ConfigBed";
    public override bool Active() => true;
    public override int NumberOfStates => 2;

    public override string DisplayValue()
        => CurrentState == 0 ? "PvPAdventure Debug Stats: On" : "PvPAdventure Debug Stats: Off";

    public override bool OnLeftClick(ref SoundStyle? sound)
    {
        DebugStatsSystem.Toggle();
        sound = DebugStatsSystem.IsVisible ? SoundID.MenuOpen : SoundID.MenuClose;
        return true;
    }

    public override bool Draw(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams)
    {
        var tex = (DebugStatsSystem.IsVisible
            ? TextureAssets.InventoryTickOn
            : TextureAssets.InventoryTickOff).Value;

        spriteBatch.Draw(tex, drawParams.Position, null, drawParams.Color,
            0f, tex.Size() * 0.5f, drawParams.Scale, SpriteEffects.None, 0f);

        return false;
    }
}

#endif
