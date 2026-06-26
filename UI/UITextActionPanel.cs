using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.UI;

public class UITextActionPanel : UIAutoScaleTextTextPanel<string>
{
    private static readonly Color EnabledBackground = new Color(72, 50, 116) * 0.88f;
    private static readonly Color DisabledBackground = new Color(46, 42, 54) * 0.85f;
    private static readonly Color DisabledBorder = new(78, 74, 88);
    private static readonly Color DisabledText = new(132, 130, 140);

    private readonly Action action;
    private readonly Action rightAction;

    /// <summary>Optional hover tooltip. Supports "\n" for multiple lines.</summary>
    public string Tooltip { get; set; }

    /// <summary>When set, returns whether the button is currently actionable. null = always enabled.</summary>
    public Func<bool> IsEnabled { get; set; }

    /// <summary>Tooltip shown instead of <see cref="Tooltip"/> while disabled — the reason it can't be clicked.</summary>
    public Func<string> DisabledTooltip { get; set; }

    public UITextActionPanel(string text, Action action, float width = 120f, float height = 40f, float textScale = 0.8f, Action rightAction = null, string tooltip = null)
        : base(text, textScale)
    {
        this.action = action;
        this.rightAction = rightAction;
        Tooltip = tooltip;
        Width.Set(width > 0f ? width : FitWidth(text, textScale), 0f);
        Height.Set(height, 0f);
        PaddingLeft = PaddingRight = 10f;
        PaddingTop = PaddingBottom = 6f;
        BackgroundColor = EnabledBackground;
        BorderColor = Color.Black;
    }

    private bool Enabled => IsEnabled?.Invoke() ?? true;

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        if (!Enabled)
            return;

        BorderColor = Color.Yellow;
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
        action?.Invoke();
    }

    public override void RightClick(UIMouseEvent evt)
    {
        base.RightClick(evt);

        if (rightAction == null)
            return;

        SoundEngine.PlaySound(SoundID.MenuTick);
        rightAction.Invoke();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsMouseHovering)
            return;

        Main.LocalPlayer.mouseInterface = true;

        string tooltip = !Enabled && DisabledTooltip != null ? DisabledTooltip.Invoke() : Tooltip;
        if (!string.IsNullOrEmpty(tooltip))
            Main.instance.MouseText(tooltip);
    }

    /// <summary>Greys the button out while it can't be used. Clicks still run the action, which posts
    /// the same chat message as before, so the disabled state is purely visual + the reason tooltip.</summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (Enabled)
        {
            BackgroundColor = EnabledBackground;
            TextColor = Color.White;
        }
        else
        {
            BackgroundColor = DisabledBackground;
            BorderColor = DisabledBorder;
            TextColor = DisabledText;
        }

        base.DrawSelf(spriteBatch);
    }

    private static float FitWidth(string text, float scale) => FontAssets.MouseText.Value.MeasureString(text).X * scale + 28f;
}
