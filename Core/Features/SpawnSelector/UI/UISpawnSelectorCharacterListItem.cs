using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.UI
{
    /// <summary>
    /// Vanilla-like character row:
    /// </summary>
    internal class UISpawnSelectorCharacterListItem : UIPanel
    {
        private readonly Asset<Texture2D> _dividerTexture;
        private readonly Asset<Texture2D> _innerPanelTexture;
        private readonly Asset<Texture2D> _playerBGTexture;

        private readonly string _name;
        private readonly string _hpText;
        private readonly string _manaText;

        public UISpawnSelectorCharacterListItem()
        {
            _dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
            _innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
            _playerBGTexture = Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground");

            _name = "Teammate";
            _hpText = "500 " + Language.GetTextValue("GameUI.PlayerLifeMax");
            _manaText = "200 " + Language.GetTextValue("GameUI.PlayerManaMax");
        }

        public override void OnInitialize()
        {
            //BorderColor = new Color(89, 116, 213) * 0.7f;
            BorderColor = Color.Black;
            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            Height.Set(72f, 0f);
            Width.Set(-20f, 1f);
            SetPadding(6f);
        }

        private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width)
        {
            spriteBatch.Draw(_innerPanelTexture.Value, position,
                new Rectangle(0, 0, 8, _innerPanelTexture.Height()), Color.White);

            spriteBatch.Draw(
                _innerPanelTexture.Value,
                new Vector2(position.X + 8f, position.Y),
                new Rectangle(8, 0, 8, _innerPanelTexture.Height()),
                Color.White,
                0f,
                Vector2.Zero,
                new Vector2((width - 16f) / 8f, 1f),
                SpriteEffects.None,
                0f
            );

            spriteBatch.Draw(
                _innerPanelTexture.Value,
                new Vector2(position.X + width - 8f, position.Y),
                new Rectangle(16, 0, 8, _innerPanelTexture.Height()),
                Color.White
            );
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            CalculatedStyle inner = GetInnerDimensions();
            var dims = GetDimensions();

            // Left "player" background
            Vector2 pos = new(inner.X, inner.Y);
            sb.Draw(_playerBGTexture.Value, pos, Color.White);

            // Right "bed" background
            Vector2 bedPos = new(pos.X+dims.Width-73, pos.Y);
            sb.Draw(_playerBGTexture.Value, bedPos, Color.White);

            // Draw bed (placeholder)
            // Future: Draw section of map with the bed
            bedPos += new Vector2(31, 31);
            Item icon = new(ItemID.Bed);
            ItemSlot.DrawItemIcon(icon, 31, sb, bedPos, 1.0f, 32f, Color.White);


            // Draw player
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            try
            {
                // Draw player
                pos += Main.screenPosition + new Vector2(16, 8);

                Player player = Main.LocalPlayer;

                // Celestial starboard (45) sucks
                if (player.wings == 45) player.wings = 0;

                Main.PlayerRenderer.DrawPlayer(Main.Camera, player, pos, player.fullRotation, player.fullRotationOrigin, 0f, 0.9f);
            }
            catch (Exception e)
            {
                //Main.NewText("Failed to draw player", Color.Red);
                Log.Error("Failed to draw player: " + e);
            }

            float leftColumnWidth = _playerBGTexture.Width(); // keep aligned to texture
            float startX = inner.X + leftColumnWidth;

            // Measure text width
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(_name);
            float centerX = inner.X + inner.Width * 0.5f;

            // Draw player name centered
            Utils.DrawBorderString(
                sb,
                _name,
                new Vector2(centerX - textSize.X * 0.5f, inner.Y - 2f),
                Color.White
            );

            // Divider
            sb.Draw(
                _dividerTexture.Value,
                new Vector2(startX+1, inner.Y + 21f),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                new Vector2((dims.X + dims.Width - startX) / 8 - 10, 1f),
                SpriteEffects.None,
                0f
            );

            // HP / MP panel
            Vector2 panelPos = new(startX + 6f, inner.Y + 29f);
            float panelWidth = 220f;
            DrawPanel(sb, panelPos, panelWidth);

            Vector2 cursor = panelPos;

            // HP
            sb.Draw(TextureAssets.Heart.Value, cursor + new Vector2(5f, 2f), Color.White);
            cursor.X += 10f + TextureAssets.Heart.Width();
            Utils.DrawBorderString(sb, _hpText, cursor + new Vector2(0f, 3f), Color.White);

            // MP
            cursor.X += 80f;
            sb.Draw(TextureAssets.Mana.Value, cursor + new Vector2(5f, 2f), Color.White);
            cursor.X += 10f + TextureAssets.Mana.Width();
            Utils.DrawBorderString(sb, _manaText, cursor + new Vector2(0f, 3f), Color.White);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            BackgroundColor = new Color(73, 92, 161);
        }
    }
}
