using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.Players;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.UI
{
    /// <summary>
    /// Vanilla-like character row.
    /// </summary>
    internal class UISpawnSelectorCharacterListItem : UIPanel
    {
        internal const float ItemWidth = 260f;
        internal const float ItemHeight = 72f;

        private readonly Asset<Texture2D> _dividerTexture;
        private readonly Asset<Texture2D> _innerPanelTexture;
        private readonly Asset<Texture2D> _playerBGTexture;

        private readonly int _playerIndex;

        public UISpawnSelectorCharacterListItem(Player player)
        {
            _dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
            _innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
            _playerBGTexture = Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground");

            _playerIndex = player.whoAmI;
        }

        public override void OnInitialize()
        {
            BorderColor = Color.Black;
            BackgroundColor = new Color(63, 82, 151) * 0.7f;

            Height.Set(ItemHeight, 0f);
            Width.Set(ItemWidth, 0f);

            SetPadding(6f);
        }

        private void DrawHeadOnMap(Player p)
        {
            float scale = Main.mapFullscreenScale;

            Vector2 headPos = new(
                ((p.position.X + p.width / 2f) / 16f - Main.mapFullscreenPos.X) * scale
                    + Main.screenWidth / 2f - 14f * scale+48,

                ((p.position.Y + p.gfxOffY + p.height / 2f) / 16f - Main.mapFullscreenPos.Y) * scale
                    + Main.screenHeight / 2f - 14f * scale+58
            );

            Color color = Main.teamColor[p.team];

            //Main.PlayerRenderer.DrawPlayerHead(
            //    Main.Camera,
            //    p,
            //    headPos,
            //    scale: 1.3f,
            //    borderColor: color
            //);
            Utils.DrawBorderString(Main.spriteBatch, p.name, headPos, Color.White);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            CalculatedStyle inner = GetInnerDimensions();

            Player player = Main.player[_playerIndex];
            var dims = GetDimensions();

            if (player == null || !player.active)
            {
                var rect2 = dims.ToRectangle();

                Utils.DrawBorderString(sb, "Unable to find player :(", rect2.Location.ToVector2() + new Vector2(50, 0), Color.White);
                return;
            }

            // Left player background
            Vector2 pos = new(inner.X, inner.Y);
            //sb.Draw(_playerBGTexture.Value, pos, Color.White);

            Rectangle rect = new((int)pos.X-5, (int)pos.Y-5, 106,72);

            DrawMapFullscreenBackground(sb, rect, player);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred,
                     BlendState.AlphaBlend,
                     SamplerState.PointClamp,
                     DepthStencilState.Default,
                     RasterizerState.CullNone,
                     null,
                     Main.UIScaleMatrix);

            ModifyPlayerDrawInfo.ForceFullBrightOnce = true;
            try
            {
                Vector2 playerDrawPos = pos + Main.screenPosition + new Vector2(32, 8);

                //Main.PlayerRenderer.DrawPlayer(Main.Camera,player,playerDrawPos,player.fullRotation,player.fullRotationOrigin,0f,0.9f);

                Color myTeamColor = Main.teamColor[Main.LocalPlayer.team];

                Main.PlayerRenderer.DrawPlayerHead(Main.Camera, player, pos+ new Vector2(40,37), scale: 1.3f, borderColor: myTeamColor);
            }
            catch (Exception e)
            {
                Log.Error("Failed to draw player: " + e);
            }
            finally
            {
                ModifyPlayerDrawInfo.ForceFullBrightOnce = false;
            }

            // Switch back to "normal" UI batch
            sb.End();
            sb.Begin(SpriteSortMode.Deferred,
                     BlendState.AlphaBlend,
                     SamplerState.LinearClamp,
                     DepthStencilState.None,
                     RasterizerState.CullCounterClockwise,
                     null,
                     Main.UIScaleMatrix);

            float leftColumnWidth = _playerBGTexture.Width();
            float startX = inner.X + leftColumnWidth;

            // Name centered
            string name = string.IsNullOrEmpty(player.name) ? "Unknown player" : player.name;
            Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name);
            float centerX = inner.X + inner.Width * 0.5f+20;

            Utils.DrawBorderString(
                sb,
                name,
                new Vector2(centerX, inner.Y),
                Color.White,
                scale: 1.1f
            );

            // Divider
            sb.Draw(
                _dividerTexture.Value,
                new Vector2(startX + 44, inner.Y + 21f),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                new Vector2((dims.X + dims.Width - startX) / 8-7, 1f),
                SpriteEffects.None,
                0f
            );

            // HP / MP panel
            Vector2 panelPos = new(startX + 60f, inner.Y + 29f);
            float panelWidth = 90f;
            DrawPanel(sb, panelPos, panelWidth);

            Vector2 cursor = panelPos;

            // HP
            sb.Draw(TextureAssets.Heart.Value, cursor + new Vector2(4f, 2f), Color.White);
            cursor.X += 6f + TextureAssets.Heart.Width();
            string hpText = $"{player.statLife} HP";
            Utils.DrawBorderString(sb, hpText, cursor + new Vector2(0f, 3f), Color.White);

            // MP
            //cursor.X += 80f;
            //sb.Draw(TextureAssets.Mana.Value, cursor + new Vector2(-15f, 0f), Color.White);
            //cursor.X += -11f + TextureAssets.Mana.Width();
            //string manaText = $"{player.statMana} MP";
            //Utils.DrawBorderString(sb, manaText, cursor + new Vector2(0f, 3f), Color.White);

            // Set hover teleport tooltip
            if (IsMouseHovering)
            {
                Main.instance.MouseText("Teleport to " + name);
                var old = Main.UIScaleMatrix;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Matrix.Identity
                );

                DrawHeadOnMap(player); 

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    old   
                );
            }
            //Main.NewText(Main.mouseY); // 482,435 is spawn

        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);

            BackgroundColor = new Color(73, 92, 161);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            Player player = Main.player[_playerIndex];

            Main.LocalPlayer.UnityTeleport(player.position);

            // close map and SSS
            Main.mapFullscreen = false;
            SpawnSelectorSystem.SetEnabled(false);
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

        public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Player player)
        {
            if (player == null || !player.active)
                return;

            var screenPos = Main.screenPosition;
            var tile = Main.tile[(int)(player.Center.X / 16f), (int)(player.Center.Y / 16f)];
            if (tile == null) return;

            int wall = tile.wall;
            int bgIndex = -1;
            Color color = Color.White;

            if (screenPos.Y > (Main.maxTilesY - 232) * 16)
                bgIndex = 2;
            else if (player.ZoneDungeon)
                bgIndex = 4;
            else if (wall == 87)
                bgIndex = 13;
            else if (screenPos.Y > Main.worldSurface * 16.0)
            {
                bgIndex = wall switch
                {
                    86 or 108 => 15,
                    180 or 184 => 16,
                    178 or 183 => 17,
                    62 or 263 => 18,
                    _ => player.ZoneGlowshroom ? 20 :
                         player.ZoneCorrupt ? player.ZoneDesert ? 39 : player.ZoneSnow ? 33 : 22 :
                         player.ZoneCrimson ? player.ZoneDesert ? 40 : player.ZoneSnow ? 34 : 23 :
                         player.ZoneHallow ? player.ZoneDesert ? 41 : player.ZoneSnow ? 35 : 21 :
                         player.ZoneSnow ? 3 :
                         player.ZoneJungle ? 12 :
                         player.ZoneDesert ? 14 :
                         player.ZoneRockLayerHeight ? 31 : 1
                };
            }
            else if (player.ZoneGlowshroom)
                bgIndex = 19;
            else
            {
                color = Main.ColorOfTheSkies;
                int midTileX = (int)((screenPos.X + Main.screenWidth / 2f) / 16f);

                if (player.ZoneSkyHeight) bgIndex = 32;
                else if (player.ZoneCorrupt) bgIndex = player.ZoneDesert ? 36 : 5;
                else if (player.ZoneCrimson) bgIndex = player.ZoneDesert ? 37 : 6;
                else if (player.ZoneHallow) bgIndex = player.ZoneDesert ? 38 : 7;
                else if (screenPos.Y / 16f < Main.worldSurface + 10.0 && (midTileX < 380 || midTileX > Main.maxTilesX - 380))
                    bgIndex = 10;
                else if (player.ZoneSnow) bgIndex = 11;
                else if (player.ZoneJungle) bgIndex = 8;
                else if (player.ZoneDesert) bgIndex = 9;
                else if (Main.bloodMoon) { bgIndex = 25; color *= 2f; }
                else if (player.ZoneGraveyard) bgIndex = 26;
            }

            var asset = bgIndex >= 0 && bgIndex < TextureAssets.MapBGs.Length
                ? TextureAssets.MapBGs[bgIndex]
                : TextureAssets.MapBGs[0];

            rect.X += 10;
            rect.Y += 10;
            rect.Width -= 20;
            rect.Height -= 20;

            sb.Draw(asset.Value, rect, color);
        }
    }
}
