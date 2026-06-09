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

    // ── Public entry points ───────────────────────────────────────────
    internal static void DrawButtons()
    {
        Texture2D back = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel").Value;
        Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder").Value;

        const int spacing = 6;
        const int startX = 10;
        const int startY = 85;
        const float labelScale = 0.68f;

        QueueText("PvPAdventure Debug Stats", new Vector2(startX, 64f), Color.Yellow);

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
        Vector2 origin = new(10f, 122f);
        float nextY = origin.Y;

        foreach (DebugStatGroup group in DebugStats.AllGroups())
        {
            if (!IsGroupEnabled(group.Header)) continue;

            string[] rows = group.BuildRows();
            DrawGroup(group.Header, group.Color, rows, new Vector2(origin.X, nextY));
            nextY += GroupHeight(rows.Length) + 14f;
        }
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

        QueueText(header, origin, headerColor, headerScale);

        float labelW = MaxLabelWidth(rows, rowScale);
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

    private static float GroupHeight(int rowCount)
    {
        const float headerGap = 19f;
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
        toggle?.CurrentState = IsVisible ? 0 : 1;
    }
}

/// <summary>
/// Hotbar builder toggle. Mirrors F6 visibility state.
/// Uses InventoryTickOn/Off textures — no custom asset required.
/// </summary>
public class DebugStatsBuilderToggle : BuilderToggle
{
    public override bool Active() => true;
    public override int NumberOfStates => 2;

    public override string DisplayValue()
        => CurrentState == 0 ? "PvPAdventure Debug Stats: On" : "PvPAdventure Debug Stats: Off";
    public override string Texture => "PvPAdventure/Assets/Custom/ConfigBed"; // arbitrary texture that won't ever be drawn
    public override bool OnLeftClick(ref SoundStyle? sound)
    {
        DebugStatsSystem.Toggle();
        sound = DebugStatsSystem.IsVisible ? SoundID.MenuOpen : SoundID.MenuClose;
        // Returning true auto-flips CurrentState (0 <-> 1), which stays in sync with IsVisible.
        return true;
    }

    public override bool Draw(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams)
    {
        var tex = (DebugStatsSystem.IsVisible
            ? TextureAssets.InventoryTickOn
            : TextureAssets.InventoryTickOff).Value;

        spriteBatch.Draw(tex, drawParams.Position, null, drawParams.Color,
            0f, tex.Size() * 0.5f, drawParams.Scale, SpriteEffects.None, 0f);

        return false; // skip default drawing
    }
}

#endif
