using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.UI;

/// <summary>
/// Draws a <see cref="UITextPanel{T}"/> with a left-click action.
/// Supports 3 variations:
/// 1. icon only -> center the icon
/// 2. text only -> center the text
/// 3. icon and text -> icon left, text in the remaining area
/// </summary>
public class UITextActionPanel : UITextPanel<string>
{
    private readonly Action leftClickAction;

    private string cachedMeasureText;
    private bool cachedMeasureLarge;
    private Vector2 cachedMeasureSize;
    private bool autoFitTextScale;
    private float autoFitBaseScale;
    private float autoFitMinScale;
    private float autoFitHorizontalPadding;

    public Texture2D icon;
    public float? MaxTextScaleOverride { get; set; }

    public UITextActionPanel(string text, Action leftClickAction, float height, float textScale = 1f, bool large = false, Texture2D icon = null) : base(text, textScale, large)
    {
        this.leftClickAction = leftClickAction;
        this.icon = icon;

        Width.Set(0f, 1f);
        Height.Set(height, 0f);
        MinHeight.Set(height, 0f);
        SetPadding(0f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        leftClickAction?.Invoke();
        base.LeftClick(evt);
    }

    public void SetTextAndFitScale(string text, float baseScale, float minScale = 0.35f, float horizontalPadding = 12f)
    {
        SetText(text);
        autoFitTextScale = true;
        autoFitBaseScale = baseScale;
        autoFitMinScale = minScale;
        autoFitHorizontalPadding = horizontalPadding;
    }

    public void ClearAutoFitTextScale()
    {
        autoFitTextScale = false;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        if (_drawPanel)
        {
            if (_needsTextureLoading)
            {
                _needsTextureLoading = false;
                LoadTextures();
            }

            Color oldBorderColor = BorderColor;
            if (IsMouseHovering)
                BorderColor = Color.Yellow;

            if (_backgroundTexture != null)
                DrawPanel(sb, _backgroundTexture.Value, BackgroundColor);

            if (_borderTexture != null)
                DrawPanel(sb, _borderTexture.Value, BorderColor);

            BorderColor = oldBorderColor;
        }

        DrawContents(sb);
    }

    private void DrawContents(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();

        string text = Text;
        if (HideContents)
        {
            if (_asterisks == null || _asterisks.Length != text.Length)
                _asterisks = new string('*', text.Length);

            text = _asterisks;
        }

        bool hasIcon = icon is not null;
        bool hasText = !string.IsNullOrEmpty(text);

        const int padding = 0;
        const int spacing = 0;

        Rectangle contentBox = new(box.X + padding, box.Y + padding, box.Width - padding * 2, box.Height - padding * 2);

        if (hasIcon && !hasText)
        {
            int iconSize = Math.Min(contentBox.Width, contentBox.Height);
            Rectangle iconBox = new(contentBox.Center.X - iconSize / 2, contentBox.Center.Y - iconSize / 2, iconSize, iconSize);
            DrawIcon(spriteBatch, iconBox);
            return;
        }

        if (!hasIcon && hasText)
        {
            DrawCenteredText(spriteBatch, contentBox, text);
            return;
        }

        if (hasIcon && hasText)
        {
            int iconSize = contentBox.Height;
            Rectangle iconBox = new(contentBox.X, contentBox.Y, iconSize, iconSize);
            Rectangle textBox = new(iconBox.Right + spacing, contentBox.Y, Math.Max(0, contentBox.Right - (iconBox.Right + spacing)), contentBox.Height);

            DrawIcon(spriteBatch, iconBox);
            DrawTextInBox(spriteBatch, textBox, text, true);
        }
    }

    private void DrawIcon(SpriteBatch sb, Rectangle iconBox)
    {
        if (icon is null || icon.Width <= 0 || icon.Height <= 0 || iconBox.Width <= 0 || iconBox.Height <= 0)
            return;

        float scale = Math.Min(iconBox.Width / (float)icon.Width, iconBox.Height / (float)icon.Height);
        Vector2 drawSize = icon.Size() * scale;
        Vector2 drawPos = iconBox.Center.ToVector2() - drawSize * 0.5f;

        sb.Draw(icon, drawPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

#if DEBUG
        //DebugDrawer.DrawRectangle(sb, this, false);
        //DebugDrawer.DrawRectangle(sb, iconBox, false);
        //DebugDrawer.DrawRectangle(sb, new Rectangle((int)drawPos.X, (int)drawPos.Y, (int)drawSize.X, (int)drawSize.Y), false);
        //Utils.DrawBorderString(sb, $"scale:\n{scale:0.00}", iconBox.TopLeft(), Color.White, 0.7f);
#endif
    }

    private void DrawCenteredText(SpriteBatch sb, Rectangle textBox, string text)
    {
        DrawTextInBox(sb, textBox, text, false);
    }

    private void DrawTextInBox(SpriteBatch sb, Rectangle textBox, string text, bool leftAlign)
    {
        Vector2 measureSize = GetMeasuredTextSize(text);
        if (measureSize.X <= 0f || measureSize.Y <= 0f || textBox.Width <= 0 || textBox.Height <= 0)
            return;

        float scaleX = textBox.Width / measureSize.X;
        float scaleY = textBox.Height / measureSize.Y;
        float maxTextScale = MaxTextScaleOverride ?? _textScale;

        if (autoFitTextScale)
        {
            float availableWidth = Math.Max(1f, textBox.Width - autoFitHorizontalPadding);
            float autoFitScale = measureSize.X > 0f ? Math.Min(autoFitBaseScale, availableWidth / measureSize.X) : autoFitBaseScale;
            maxTextScale = Math.Max(autoFitMinScale, autoFitScale);
            //Log.Chat(autoFitScale);
        }

        float drawScale = Math.Min(maxTextScale, Math.Min(scaleX, scaleY));

        Vector2 drawSize = measureSize * drawScale;
        Vector2 pos = new(leftAlign ? textBox.X : textBox.X + (textBox.Width - drawSize.X) * 0.5f, textBox.Y + (textBox.Height - drawSize.Y) * 0.5f);

        if (_isLarge)
            pos.Y += 8f * drawScale;
        else
            pos.Y -= 1f * drawScale;

        if (_isLarge)
            Utils.DrawBorderStringBig(sb, text, pos, _color, drawScale);
        else
            Utils.DrawBorderString(sb, text, pos, _color, drawScale);

#if DEBUG
        //DebugDrawer.DrawElement(sb, this);
        //DebugDrawer.DrawRectangle(sb, textBox);
        //Log.Chat($"text='{text}' box={box.Width}x{box.Height} measure={measureSize.X:0.00}x{measureSize.Y:0.00} draw={drawSize.X:0.00}x{drawSize.Y:0.00} pos=({pos.X:0.00},{pos.Y:0.00})");
#endif
    }

    private Vector2 GetMeasuredTextSize(string text)
    {
        if (cachedMeasureText == text && cachedMeasureLarge == _isLarge)
            return cachedMeasureSize;

        DynamicSpriteFont font = _isLarge ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;
        cachedMeasureText = text;
        cachedMeasureLarge = _isLarge;
        cachedMeasureSize = font.MeasureString(text);

        return cachedMeasureSize;
    }
}
