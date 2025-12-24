using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.UI;

internal class SliderElement : UIElement
{
    private readonly string labelTextKey;
    public UIText Label;
    public Slider Slider;
    public float Min { get; }
    public float Max { get; }
    private readonly float step;
    private readonly Action<float> onValueChangedCallback;
    private float appliedValue;

    public SliderElement(string label, float min, float max, float defaultValue, float step = 0.01f, Action<float> onValueChanged = null)
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
            SoundEngine.PlaySound(SoundID.MenuTick);
            Label.TextColor = Color.White;
        };
        Label.OnMouseOut += (_, _) =>
        {
            Label.TextColor = Color.Gray;
        };

        Slider = new Slider
        {
            Left = { Percent = 0f, Pixels = -3f },
            Width = { Percent = 0.50f, Pixels = 0f },
            VAlign = 0.5f,
            HAlign = 1f
        };

        appliedValue = MathHelper.Clamp(defaultValue, Min, Max);
        Slider.Ratio = (appliedValue - Min) / (Max - Min);
        Slider.OnDrag += HandleSliderDrag;
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
        // Here we perform special value formatting for SliderElements that handles time values.
        if(labelTextKey == "Time")
        {
            int totalMinutes = Math.Max(0, (int)appliedValue);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            string part;
            if (hours > 0 && minutes > 0)
                part = $"{hours} hr{(hours == 1 ? "" : "s")} {minutes} min{(minutes == 1 ? "" : "s")}";
            else if (hours > 0)
                part = $"{hours} hr{(hours == 1 ? "" : "s")}";
            else
                part = $"{minutes} min{(minutes == 1 ? "" : "s")}";

            Label.SetText($"Time: {part}");
            return;
        }

        int intVal = (int)appliedValue;
        Label.SetText($"{labelTextKey}: {intVal}");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
