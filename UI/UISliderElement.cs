using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.UI;

internal class UISliderElement : UIElement
{
    private readonly string labelTextKey;
    public UIText Label;
    public UISlider Slider;
    public float Min { get; }
    public float Max { get; }
    private readonly float step;
    private readonly Action<float> onValueChangedCallback;
    public Action<float> OnRelease { get; set; }
    private float appliedValue;
    private bool enabled = true;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            Slider.Enabled = value;
            Label.TextColor = value ? Color.Gray : Color.DimGray;
        }
    }

    public bool ShowDisabledLockIcon { get; set; }
    public string DisabledTooltip { get; set; }

    public UISliderElement(string label, float min, float max, float defaultValue, float step = 0.01f, Action<float> onValueChanged = null)
    {
        Min = min;
        Max = max;
        labelTextKey = label;
        this.step = step;
        onValueChangedCallback = onValueChanged;

        Width.Set(0, 1f);
        Height.Set(23, 0);

        Label = new UIText("", 1f, false)
        {
            Left = { Pixels = 0 },
            VAlign = 0.5f,
            TextOriginX = 0f,
            TextOriginY = 0.5f,
            TextColor = Color.Gray
        };
        Label.Width.Set(-150, 1f);
        Append(Label);

        Label.OnMouseOver += (_, _) =>
        {
            if (!Enabled)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            Label.TextColor = Color.White;
        };
        Label.OnMouseOut += (_, _) =>
        {
            Label.TextColor = Enabled ? Color.Gray : Color.DimGray;
        };

        Slider = new UISlider
        {
            Left = { Percent = 0f, Pixels = -3f },
            Width = { Percent = 0.50f, Pixels = 0f },
            VAlign = 0.5f,
            HAlign = 1f
        };

        appliedValue = MathHelper.Clamp(defaultValue, Min, Max);
        Slider.Ratio = (appliedValue - Min) / (Max - Min);
        Slider.OnDrag += HandleSliderDrag;
        Slider.OnRelease += ratio =>
        {
            float rawValue = Min + ratio * (Max - Min);
            float snapped = MathHelper.Clamp(
                (float)Math.Round((rawValue - Min) / step) * step + Min, Min, Max);
            OnRelease?.Invoke(snapped);
        };
        UpdateLabelText();

        Append(Slider);
    }

    private void HandleSliderDrag(float currentRatio)
    {
        float rawValue = Min + currentRatio * (Max - Min);
        float newSnappedValue = (float)Math.Round((rawValue - Min) / step) * step + Min;
        newSnappedValue = MathHelper.Clamp(newSnappedValue, Min, Max);

        if (Math.Abs(appliedValue - newSnappedValue) > float.Epsilon)
        {
            appliedValue = newSnappedValue;
            onValueChangedCallback?.Invoke(appliedValue);
            UpdateLabelText();
        }
    }

    private void UpdateLabelText()
    {
        // Special formatting for time to format as minutes/hours (including negative).
        //bool formatAsTime = step >= 1f && (Math.Abs(Min) >= 60f || Max >= 60f);
        if (labelTextKey == "Time")
        {
            int totalMinutes = (int)Math.Round(appliedValue);
            bool isNegative = totalMinutes < 0;
            int absMinutes = Math.Abs(totalMinutes);
            int hours = absMinutes / 60;
            int minutes = absMinutes % 60;

            string part;
            if (hours > 0 && minutes > 0)
                part = $"{hours} hr{(hours == 1 ? "" : "s")} {minutes} min{(minutes == 1 ? "" : "s")}";
            else if (hours > 0)
                part = $"{hours} hr{(hours == 1 ? "" : "s")}";
            else
                part = $"{minutes} min{(minutes == 1 ? "" : "s")}";

            string sign = isNegative ? "-" : string.Empty;
            Label.SetText($"{labelTextKey}: {sign}{part}");
            return;
        }

        //if (labelTextKey == "Countdown")  // format as m:ss
        //{
        //    int totalSeconds = (int)Math.Round(appliedValue);
        //    int m = totalSeconds / 60;
        //    int s = totalSeconds % 60;
        //    string formatted = m > 0 ? $"{m}:{s:D2}" : $"{s}s";
        //    Label.SetText($"{labelTextKey}: {formatted}");
        //    return;
        //}

        int intVal = (int)Math.Round(appliedValue);
        Label.SetText($"{labelTextKey}: {intVal}");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (Enabled)
            return;

        Rectangle rect = GetDimensions().ToRectangle();
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color.Black * 0.35f);

        if (!ShowDisabledLockIcon || Ass.IconLock?.Value == null)
            return;

        Texture2D lockTexture = Ass.IconLock.Value;
        const int lockSize = 20;
        Rectangle lockRect = new(rect.Right - lockSize - 4, rect.Center.Y - lockSize / 2, lockSize, lockSize);
        spriteBatch.Draw(lockTexture, lockRect, Color.White * 0.85f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!Enabled && IsMouseHovering && !string.IsNullOrWhiteSpace(DisabledTooltip))
            Main.instance.MouseText(DisabledTooltip);
    }

    // For live game countdown, we set the slider value directly
    public void SetValue(float value)
    {
        float clamped = MathHelper.Clamp(value, Min, Max);
        Slider.Ratio = (clamped - Min) / (Max - Min);
        appliedValue = clamped;
        UpdateLabelText();
    }
}
