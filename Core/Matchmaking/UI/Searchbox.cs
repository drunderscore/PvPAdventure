using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Matchmaking.UI
{
    //ty jopojelly and darthmorf
    public class Searchbox : UIPanel
    {
        internal string currentString = string.Empty;

        internal bool focused = false;

        private readonly int _maxLength = 20;

        private readonly string hintText;
        private int textBlinkerCount;
        private int textBlinkerState;
        public float Scale;

        public event Action OnFocus;

        public event Action OnUnfocus;

        public event Action OnTextChanged;

        public event Action OnTabPressed;

        public event Action OnEnterPressed;

        internal bool unfocusOnEnter = true;

        internal bool unfocusOnTab = true;

        internal Searchbox(string hintText, string text = "")
        {
            this.hintText = hintText;
            currentString = text;
            SetPadding(0);
            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = Color.Black;
        }

        public override bool ContainsPoint(Vector2 point)
        {
            bool isInPoint = base.ContainsPoint(point);

            if (isInPoint && Main.mouseLeft)
            {
                Main.mouseLeftRelease = false;
                Focus();
            }

            return isInPoint;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            Focus();
            base.LeftClick(evt);
        }

        internal void Unfocus()
        {
            if (focused)
            {
                focused = false;
                Main.blockInput = false;

                OnUnfocus?.Invoke();
            }
        }

        internal void Focus()
        {
            if (!focused)
            {
                Main.clrInput();
                focused = true;
                Main.blockInput = true;

                OnFocus?.Invoke();
            }
        }

        public override void Update(GameTime gameTime)
        {
            // if (IsMouseHovering)
            // {
            //     Log.Info("Mouse is hovering over searchbox");
            // }
            // else
            // {
            //     Log.Info("Not hovering over searchbox");
            // }

            Vector2 MousePosition = new(Main.mouseX, Main.mouseY);
            if (!ContainsPoint(MousePosition) && (Main.mouseLeft || Main.mouseRight)) //This solution is fine, but we need a way to cleanly "unload" a UIElement
            {
                //TODO, figure out how to refocus without triggering unfocus while clicking enable button.
                Unfocus();
            }
            base.Update(gameTime);
        }

        internal void SetText(string text)
        {
            if (text.Length > _maxLength)
            {
                text = text.Substring(0, _maxLength);
            }
            if (currentString != text)
            {
                currentString = text;
                OnTextChanged?.Invoke();
            }
        }

        private static bool JustPressed(Keys key)
        {
            return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            // Panel background etc.
            base.DrawSelf(sb);

            if (focused)
            {
                Terraria.GameInput.PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newString = Main.GetInputText(currentString);
                if (!string.Equals(newString, currentString))
                {
                    currentString = newString;
                    OnTextChanged?.Invoke();
                }

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

                textBlinkerCount++;
                if (textBlinkerCount >= 20)
                {
                    textBlinkerState = (textBlinkerState + 1) % 2;
                    textBlinkerCount = 0;
                }

                Main.instance.DrawWindowsIMEPanel(new Vector2(98f, Main.screenHeight - 36), 0f);
            }

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 drawPos = GetDimensions().Position() + new Vector2(8f, 3f);

            bool hasText = !string.IsNullOrEmpty(currentString);
            string textToDraw = hasText ? currentString : hintText;

            if (hasText) drawPos.X += 3f;

            // Cursor only when there is actual text
            if (focused && textBlinkerState == 1 && hasText)
                textToDraw += "|";

            float scale = hasText ? 1f : 1.0f;
            Color innerColor = hasText ? Color.White : Color.DimGray;
            Color outlineColor = Color.Black;

            Vector2[] offsets =
            [
                new(-1f, -1f),
                new( 1f, -1f),
                new(-1f,  1f),
                new( 1f,  1f)
            ];

            // Outline
            for (int i = 0; i < offsets.Length; i++)
            {
                sb.DrawString(font,textToDraw,drawPos + offsets[i],outlineColor,0f,Vector2.Zero,scale,SpriteEffects.None,0f);
            }

            // Fill
            sb.DrawString(font,textToDraw,drawPos,innerColor,0f,Vector2.Zero, scale,SpriteEffects.None, 0f);
        }

    }
}