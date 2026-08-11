using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.UI;

/// <summary>
/// A single "+" or "-" step button with the shared PvPAdventure / ErkySSC style.
/// IMPORTANT: keep this class identical to ErkySSC's UIPlusMinusButton.
/// </summary>
public class UIPlusMinusButton : UIElement
{
    public const float DefaultSize = 20f;

    private readonly string text;
    private readonly Action onClick;
    private readonly Func<bool> isEnabled;
    private bool wasHovered;

    public UIPlusMinusButton(string text, Action onClick, Func<bool> isEnabled = null)
    {
        this.text = text;
        this.onClick = onClick;
        this.isEnabled = isEnabled;

        Width.Set(DefaultSize, 0f);
        Height.Set(DefaultSize, 0f);
    }

    private bool Enabled => isEnabled?.Invoke() ?? true;

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (Enabled)
            onClick?.Invoke();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsMouseHovering)
        {
            wasHovered = false;
            return;
        }

        Main.LocalPlayer.mouseInterface = true;

        if (!wasHovered && Enabled)
            SoundEngine.PlaySound(SoundID.MenuTick);

        wasHovered = true;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        bool enabled = Enabled;
        bool hovered = enabled && IsMouseHovering;

        Color bg = enabled
            ? hovered ? new Color(90, 130, 220, 240) : new Color(55, 74, 140, 220)
            : new Color(86, 86, 96, 155);
        Color textColor = enabled ? Color.White : Color.Gray;

        DrawBar(sb, PvPOnline.Core.Utilities.Ass.SliderButton.Value, rect, bg);

        if (hovered)
            DrawBar(sb, PvPOnline.Core.Utilities.Ass.SliderButtonHighlight.Value, rect, Color.Yellow);

        const float scale = 0.75f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(
            (float)Math.Round(rect.Center.X - textSize.X * 0.5f),
            (float)Math.Round(rect.Center.Y - textSize.Y * 0.5f) + 2f); // nudge text down 2px
        Utils.DrawBorderString(sb, text, position, textColor, scale);
    }

    private static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
    {
        if (texture == null)
            return;

        spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, 6, dimensions.Height), new Rectangle(0, 0, 6, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + 6, dimensions.Y, dimensions.Width - 12, dimensions.Height), new Rectangle(6, 0, 2, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + dimensions.Width - 6, dimensions.Y, 6, dimensions.Height), new Rectangle(8, 0, 6, texture.Height), color);
    }
}
