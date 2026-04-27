using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.UI;

public class UISearchbox : UIPanel
{
    internal string currentString = string.Empty;
    internal bool focused;
    internal bool unfocusOnEnter = true;
    internal bool unfocusOnTab = true;

    private const int MaxLength = 20;
    private readonly UIImageButton clearButton;
    private string hintText;
    private int textBlinkerCount;
    private int textBlinkerState;

    public float Scale;
    public event Action OnFocus, OnUnfocus, OnTextChanged, OnTabPressed, OnEnterPressed;

    internal UISearchbox(string hintText = "", string text = "")
    {
        this.hintText = hintText;
        currentString = text;
        SetPadding(0);
        BackgroundColor = Color.White;
        BorderColor = Color.Black;

        clearButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel"));
        clearButton.Width.Set(20f, 0f);
        clearButton.Height.Set(20f, 0f);
        clearButton.OnLeftClick += (_, _) =>
        {
            SetText(string.Empty);
            Focus();
        };

        Append(clearButton);
    }

    internal void SetHintText(string text) => hintText = text ?? string.Empty;

    public override bool ContainsPoint(Vector2 point)
    {
        bool contains = base.ContainsPoint(point);

        if (contains && Main.mouseLeft)
        {
            Main.mouseLeftRelease = false;
            Focus();
        }

        return contains;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        Focus();
        base.LeftClick(evt);
    }

    internal void Focus()
    {
        if (focused)
            return;

        Main.clrInput();
        focused = true;
        Main.blockInput = true;
        OnFocus?.Invoke();
    }

    internal void Unfocus()
    {
        if (!focused)
            return;

        focused = false;
        Main.blockInput = false;
        OnUnfocus?.Invoke();
    }

    public override void Update(GameTime gameTime)
    {
        if (!ContainsPoint(new Vector2(Main.mouseX, Main.mouseY)) && (Main.mouseLeft || Main.mouseRight))
            Unfocus();

        base.Update(gameTime);
    }

    internal void SetText(string text)
    {
        text = text.Length > MaxLength ? text[..MaxLength] : text;

        if (currentString == text)
            return;

        currentString = text;
        OnTextChanged?.Invoke();
    }

    private static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);
        HandleTextInput();

        Rectangle box = GetInnerDimensions().ToRectangle();
        clearButton.Left.Set(box.Width - 26f, 0f);
        clearButton.Top.Set((box.Height - 24f) * 0.5f, 0f);
        clearButton.Recalculate();

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        bool hasText = !string.IsNullOrEmpty(currentString);
        string text = hasText ? currentString : hintText;
        float scale = hasText ? 1f : 0.9f;
        Vector2 position = new(box.X, box.Y + (box.Height - font.MeasureString(text).Y * scale) * 0.5f);
        // extra offset
        Vector2 extraCustomOffset = new(8, 2);
        position += extraCustomOffset;

        if (!hasText)
        {
            sb.DrawString(font, hintText, position, Color.DimGray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (focused && textBlinkerState == 1)
                DrawOutlinedText(sb, font, "|", position, 1f);

            return;
        }

        if (focused && textBlinkerState == 1)
            text += "|";

        while (text.Length > 0 && font.MeasureString(text).X > box.Width - 34f)
            text = text[..^1];

        DrawOutlinedText(sb, font, text, position, 1f);
    }

    private void HandleTextInput()
    {
        if (!focused)
            return;

        Terraria.GameInput.PlayerInput.WritingText = true;
        Main.instance.HandleIME();
        SetText(Main.GetInputText(currentString));

        if (JustPressed(Keys.Tab))
        {
            if (unfocusOnTab)
                Unfocus();

            OnTabPressed?.Invoke();
        }

        if (JustPressed(Keys.Enter))
        {
            Main.drawingPlayerChat = false;

            if (unfocusOnEnter)
                Unfocus();

            OnEnterPressed?.Invoke();
        }

        if (++textBlinkerCount >= 20)
        {
            textBlinkerState = (textBlinkerState + 1) % 2;
            textBlinkerCount = 0;
        }

        Main.instance.DrawWindowsIMEPanel(new Vector2(98f, Main.screenHeight - 36), 0f);
    }

    private static void DrawOutlinedText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, float scale)
    {
        Vector2[] offsets = [new(-1f, -1f), new(1f, -1f), new(-1f, 1f), new(1f, 1f)];

        for (int i = 0; i < offsets.Length; i++)
            sb.DrawString(font, text, position + offsets[i], Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        sb.DrawString(font, text, position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}