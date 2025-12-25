//using System;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using ReLogic.Graphics;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.GameContent.UI.Elements;
//using Terraria.UI;

//namespace PvPAdventure.Common.Integrations.SharedUI;

////ty jopojelly and darthmorf
//public class Searchbox : UIPanel
//{
//    internal string currentString = string.Empty;

//    internal bool focused = false;

//    private readonly int _maxLength = 20;

//    private readonly string hintText;
//    private int textBlinkerCount;
//    private int textBlinkerState;
//    public float Scale;

//    public event Action OnFocus;

//    public event Action OnUnfocus;

//    public event Action OnTextChanged;

//    public event Action OnTabPressed;

//    public event Action OnEnterPressed;

//    internal bool unfocusOnEnter = true;

//    internal bool unfocusOnTab = true;

//    internal Searchbox(string hintText, string text = "")
//    {
//        this.hintText = hintText;
//        currentString = text;
//        SetPadding(0);
//        BackgroundColor = Color.White;
//        BorderColor = Color.Black;

//        // more:
//        BackgroundColor = Color.White;
//        BorderColor = Color.Black;

//        // parent container will do positioning for us.
//    }

//    public override bool ContainsPoint(Vector2 point)
//    {
//        bool isInPoint = base.ContainsPoint(point);

//        if (isInPoint && Main.mouseLeft)
//        {
//            Main.mouseLeftRelease = false;
//            Focus();
//        }

//        return isInPoint;
//    }

//    public override void LeftClick(UIMouseEvent evt)
//    {
//        Focus();
//        base.LeftClick(evt);
//    }

//    internal void Unfocus()
//    {
//        if (focused)
//        {
//            focused = false;
//            Main.blockInput = false;

//            OnUnfocus?.Invoke();
//        }
//    }

//    internal void Focus()
//    {
//        if (!focused)
//        {
//            Main.clrInput();
//            focused = true;
//            Main.blockInput = true;

//            OnFocus?.Invoke();
//        }
//    }

//    public override void Update(GameTime gameTime)
//    {
//        Vector2 MousePosition = new(Main.mouseX, Main.mouseY);
//        if (!ContainsPoint(MousePosition) && (Main.mouseLeft || Main.mouseRight)) //This solution is fine, but we need a way to cleanly "unload" a UIElement
//        {
//            //TODO, figure out how to refocus without triggering unfocus while clicking enable button.
//            Unfocus();
//        }
//        base.Update(gameTime);
//    }

//    internal void SetText(string text)
//    {
//        if (text.Length > _maxLength)
//        {
//            text = text.Substring(0, _maxLength);
//        }
//        if (currentString != text)
//        {
//            currentString = text;
//            OnTextChanged?.Invoke();
//        }
//    }

//    private static bool JustPressed(Keys key)
//    {
//        return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
//    }

//    protected override void DrawSelf(SpriteBatch sb)
//    {
//        Rectangle hitbox = GetInnerDimensions().ToRectangle();

//        // Debug
//        //sb.Draw(TextureAssets.MagicPixel.Value, hitbox, Color.Red * 0.5f);

//        // Draw panel
//        base.DrawSelf(sb);

//        if (focused)
//        {
//            Terraria.GameInput.PlayerInput.WritingText = true;
//            Main.instance.HandleIME();
//            string newString = Main.GetInputText(currentString);
//            if (!newString.Equals(currentString))
//            {
//                currentString = newString;
//                OnTextChanged?.Invoke();
//            }
//            else
//            {
//                currentString = newString;
//            }

//            if (JustPressed(Keys.Tab))
//            {
//                if (unfocusOnTab) Unfocus();
//                OnTabPressed?.Invoke();
//            }
//            if (JustPressed(Keys.Enter))
//            {
//                Main.drawingPlayerChat = false;
//                if (unfocusOnEnter) Unfocus();
//                OnEnterPressed?.Invoke();
//            }
//            if (++textBlinkerCount >= 20)
//            {
//                textBlinkerState = (textBlinkerState + 1) % 2;
//                textBlinkerCount = 0;
//            }
//            Main.instance.DrawWindowsIMEPanel(new Vector2(98f, Main.screenHeight - 36), 0f);
//        }

//        // Build blinking string for non-empty case
//        string displayString = currentString;
//        if (textBlinkerState == 1 && focused && !string.IsNullOrEmpty(currentString))
//            displayString += "|";

//        CalculatedStyle space = GetDimensions();
//        Vector2 drawPos = space.Position() + new Vector2(8, 6);
//        DynamicSpriteFont font = FontAssets.MouseText.Value;
//        const float SCALE = 1.0f;

//        Vector2[] offsets =
//        [
//            new(-1, -1),
//            new(1, -1),
//            new(-1, 1),
//            new(1, 1)
//        ];

//        if (string.IsNullOrEmpty(currentString))
//        {
//            // Draw hint text
//            const float HINT_SCALE = 0.9f;
//            sb.DrawString(font, hintText, drawPos, Color.DimGray, 0f, Vector2.Zero, HINT_SCALE, SpriteEffects.None, 0f);

//            // Draw cursor at start with black stroke + white fill
//            if (focused && textBlinkerState == 1)
//            {
//                string cursor = "|";
//                Color outlineColor = Color.Black;
                
//                foreach (var offset in offsets)
//                {
//                    sb.DrawString(font, cursor, drawPos + offset, outlineColor, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
//                }
//                sb.DrawString(font, cursor, drawPos, Color.White, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
//            }
//        }
//        else
//        {
//            // Draw outline + text (with cursor at end if blinking)
//            Color outlineColor = Color.Black;
//            foreach (var offset in offsets)
//            {
//                sb.DrawString(font, displayString, drawPos + offset, outlineColor, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
//            }

//            sb.DrawString(font, displayString, drawPos, Color.White, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
//        }

//    }
//}