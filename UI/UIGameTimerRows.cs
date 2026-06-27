using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.UI;

public class UIGameManagerSliderRow : UIElement
{
    private readonly string title;
    private readonly Texture2D icon;
    private readonly float iconScale;

    public readonly UIGameManagerSlider Slider;

    public UIGameManagerSliderRow(string title, Texture2D icon, float min, float max, float value, float step, Action<float> onChange, Action<float> onRelease, Func<float, string> format, float buttonStep = 0f, float iconScale = 1f)
    {
        this.title = title;
        this.icon = icon;
        this.iconScale = iconScale;
        Width.Set(0f, 1f);
        Height.Set(72f, 0f);

        Slider = new UIGameManagerSlider(min, max, value, step, onChange, onRelease, format, buttonStep)
        {
            Left = { Pixels = 16f },
            Top = { Pixels = 39f },
            Width = { Percent = 1f, Pixels = -36f }
        };
        Append(Slider);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        float x = rect.X + 12f;

        if (icon != null)
        {
            DrawIcon(spriteBatch, icon, new Rectangle(rect.X + 9, rect.Y + 8, 26, 26), iconScale);
            x += 30f;
        }

        Utils.DrawBorderString(spriteBatch, title, new Vector2(x, rect.Y + 10f), Color.White, 0.82f);
    }

    public static void DrawIcon(SpriteBatch spriteBatch, Texture2D texture, Rectangle box, float scaleMul = 1f)
    {
        if (texture == null || texture.Width <= 0 || texture.Height <= 0 || box.Width <= 0 || box.Height <= 0)
            return;

        float scale = Math.Min(1f, Math.Min(box.Width / (float)texture.Width, box.Height / (float)texture.Height)) * scaleMul;
        Vector2 size = texture.Size() * scale;
        spriteBatch.Draw(texture, box.Center.ToVector2() - size * 0.5f, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}

/// <summary>An icon + title header row, matching the header styling of <see cref="UIGameManagerSliderRow"/>.</summary>
public class UIIconLabelRow : UIElement
{
    private readonly string title;
    private readonly Texture2D icon;
    private readonly float iconScale;

    public UIIconLabelRow(string title, Texture2D icon, float height = 36f, float iconScale = 1f)
    {
        this.title = title;
        this.icon = icon;
        this.iconScale = iconScale;
        Width.Set(0f, 1f);
        Height.Set(height, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        float x = rect.X + 12f;

        if (icon != null)
        {
            UIGameManagerSliderRow.DrawIcon(spriteBatch, icon, new Rectangle(rect.X + 9, rect.Y + 8, 26, 26), iconScale);
            x += 30f;
        }

        Utils.DrawBorderString(spriteBatch, title, new Vector2(x, rect.Y + 10f), Color.White, 0.82f);
    }
}

public class UIGameManagerSlider : UIElement
{
    private const float ButtonWidth = 20f;
    private const float ButtonHeight = 20f;
    private const float Gap = 2f;
    private const float ValueWidth = 92f;
    private const float SliderHeight = 16f;
    private const float SliderLeftOffset = ButtonWidth + Gap;
    private const float SliderRightOffset = Gap + ButtonWidth + Gap + ValueWidth;
    private const float TextScale = 0.66f;

    private readonly float step;
    private readonly float buttonStep;
    private readonly Action<float> onValueChanged;
    private readonly Action<float> onRelease;
    private readonly Func<float, string> formatValue;
    private float value;
    private bool enabled = true;

    public readonly UISlider Slider;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            Slider.Enabled = value;
        }
    }

    public bool IsHeld => Slider.IsHeld;

    public UIGameManagerSlider(float min, float max, float value, float step, Action<float> onValueChanged, Action<float> onRelease, Func<float, string> formatValue, float buttonStep = 0f)
    {
        Min = min;
        Max = max;
        this.step = step;
        this.buttonStep = buttonStep;
        this.onValueChanged = onValueChanged;
        this.onRelease = onRelease;
        this.formatValue = formatValue;

        Width.Set(0f, 1f);
        Height.Set(ButtonHeight, 0f);

        Slider = new UISlider
        {
            Left = { Pixels = SliderLeftOffset },
            Top = { Pixels = (ButtonHeight - SliderHeight) * 0.5f },
            Width = { Percent = 1f, Pixels = -(SliderLeftOffset + SliderRightOffset) },
            Height = { Pixels = SliderHeight }
        };
        Slider.OnDrag += ratio => Apply(Min + ratio * (Max - Min), notify: true, release: false);
        Slider.OnRelease += _ => onRelease?.Invoke(this.value);
        Append(Slider);

        Append(new UIPlusMinusButton("-", () => Step(-1), () => Enabled && CanStep(-1))
        {
            Left = { Pixels = 0f },
            VAlign = 0.5f
        });

        Append(new UIPlusMinusButton("+", () => Step(1), () => Enabled && CanStep(1))
        {
            Left = { Percent = 1f, Pixels = -(ValueWidth + Gap + ButtonWidth) },
            VAlign = 0.5f
        });

        SetValue(value);
    }

    public float Min { get; }
    public float Max { get; }

    public void SetValue(float newValue) => Apply(newValue, notify: false, release: false);

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        DrawValue(spriteBatch, GetValueRect(GetDimensions().ToRectangle()));
    }

    private void Step(int direction)
    {
        if (!Enabled || !CanStep(direction))
            return;

        if (Apply(value + ButtonStepSize() * direction, notify: true, release: true))
            SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private bool Apply(float rawValue, bool notify, bool release)
    {
        float next = MathHelper.Clamp((float)Math.Round((rawValue - Min) / StepSize()) * StepSize() + Min, Min, Max);
        Slider.Ratio = Max <= Min ? 0f : (next - Min) / (Max - Min);

        if (Math.Abs(value - next) <= float.Epsilon)
            return false;

        value = next;
        if (notify)
            onValueChanged?.Invoke(value);
        if (release)
            onRelease?.Invoke(value);
        return true;
    }

    private bool CanStep(int direction) => direction < 0 ? value > Min + 0.0001f : value < Max - 0.0001f;

    private float StepSize() => step > 0f ? step : 0.01f;

    private float ButtonStepSize() => buttonStep > 0f ? buttonStep : StepSize();

    private Rectangle GetValueRect(Rectangle rect) => new((int)(rect.Right - ValueWidth), rect.Y, (int)ValueWidth, rect.Height);

    private void DrawValue(SpriteBatch spriteBatch, Rectangle rect)
    {
        DrawCenteredText(spriteBatch, formatValue?.Invoke(value) ?? value.ToString("0.##"), rect, Enabled ? Color.White : Color.Gray, TextScale);
    }

    private static void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle rect, Color color, float scale)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 size = font.MeasureString(text) * scale;
        Utils.DrawBorderString(spriteBatch, text, rect.Center.ToVector2() - size * 0.5f, color, scale);
    }
}
