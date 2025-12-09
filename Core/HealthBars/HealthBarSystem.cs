using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.HealthBars;

/// <summary>
/// Draws health bars above the players
/// </summary>
[Autoload(Side = ModSide.Client)]
public class HealthbarSystem : ModSystem
{
    // Settings
    public bool IsActive = true;
    public float Scale = 1f; 
    public int Offset = 0; // y offset
    public string Style = "Fancy"; // health bar overlay style

    // Textures
    Asset<Texture2D> _panelLeft;
    Asset<Texture2D> _panelMiddleHP;
    Asset<Texture2D> _hpFill;
    public override void PostSetupContent()
    {
        string text = "Images\\UI\\PlayerResourceSets\\HorizontalBars";
        _panelLeft = Main.Assets.Request<Texture2D>(text + "\\Panel_Left");
        _panelMiddleHP = Main.Assets.Request<Texture2D>(text + "\\HP_Panel_Middle");
        _hpFill = Main.Assets.Request<Texture2D>(text + "\\HP_Fill");
    }
    public override void OnWorldLoad()
    {
        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        IsActive = cfg.ShowHealthBars;
        Style = cfg.Theme;
    }
    public override void OnWorldUnload()
    {
        IsActive = true; // default
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        if (index != -1)
        {
            layers.Insert(index, new HealthBarLayer());
        }
    }

    private class HealthBarLayer : GameInterfaceLayer
    {
        public HealthBarLayer()
            : base("PvPAdventure:HealthBarLayer", InterfaceScaleType.Game)
        {
        }
        protected override bool DrawSelf()
        {
            var sys = ModContent.GetInstance<HealthbarSystem>();
            var cfg = ModContent.GetInstance<AdventureClientConfig>();

            if (!sys.IsActive || !cfg.ShowHealthBars)
                return true;

            // Settings
            float scale = sys.Scale;
            int offset = sys.Offset;
            var sb = Main.spriteBatch;

            // Draw health bar for every player
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.statLife <= 0) continue;

                if (sys.Style == "Vanilla")
                    DrawVanillaHealthbar(player);
                else
                    DrawHealthBar(player, scale, offset, sb);
            }

            return true;
        }

        private void DrawHealthBar(Player player, float scale, int offset, SpriteBatch sb)
        {
            const int HpPerSeg = 100;
            const int MaxSegments = 5;
            const float MidW = 12f;
            const float LeftW = 6f;
            const float BarH = 24f;
            const float BaseInnerW = MidW * MaxSegments;

            int maxLife = player.statLifeMax2;

            int segs = Utils.Clamp(maxLife / HpPerSeg + 1, 1, MaxSegments);
            float maxPerSeg = maxLife / (float)segs;

            var sys = ModContent.GetInstance<HealthbarSystem>();
            var cfg = ModContent.GetInstance<AdventureClientConfig>();

            Texture2D leftBG = null;
            Texture2D midBG = null;
            Texture2D overlay = null;

            string Style = sys.Style;
            if (Style == "Fancy")
            {
                leftBG = sys._panelLeft.Value;
                midBG = sys._panelMiddleHP.Value;
            }
            else if (Style == "Golden")
            {
                leftBG = Ass.HP_Golden_Left.Value;
                midBG = Ass.HP_Golden_Mid.Value;
                overlay = Ass.HP_Golden.Value;
            }
            else if (Style == "Leaf")
            {
                leftBG = Ass.HP_Leaf_Left.Value;
                midBG = Ass.HP_Leaf_Mid.Value;
                overlay = Ass.HP_Leaf.Value;
            }

            if (leftBG == null || midBG == null)
            {
                return;
            }

            Texture2D fillTex = sys._hpFill.Value;

            float innerW = BaseInnerW * scale;                  
            float totalW = (LeftW * 2f + BaseInnerW) * scale;  
            float totalH = BarH * scale;

            Vector2 pos = new Vector2(
                player.Center.X - totalW / 2f,
                player.Top.Y + offset - totalH - 10f * scale
            );
            pos -= Main.screenPosition;
            pos = pos.ToPoint().ToVector2();

            // BACKGROUND
            Vector2 drawPos = pos;
            sb.Draw(leftBG, drawPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            float innerX = pos.X + LeftW * scale;
            float midDestW = innerW / segs;
            float midDestH = BarH * scale;
            Rectangle srcMid = new(0, 0, midBG.Width, midBG.Height);

            for (int i = 0; i < segs; i++)
            {
                Rectangle destMid = new Rectangle(
                    (int)(innerX + i * midDestW),
                    (int)pos.Y,
                    (int)midDestW,
                    (int)midDestH
                );
                sb.Draw(midBG, destMid, srcMid, Color.White);
            }

            Vector2 rightPos = new Vector2(innerX + innerW, pos.Y);
            sb.Draw(leftBG, rightPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.FlipHorizontally, 0f);

            // FILL
            float segW = midDestW;
            float fillH = fillTex.Height * scale;
            float fillY = pos.Y + (BarH * scale - fillH) * 0.5f;
            int srcCoreW = fillTex.Width - 2;

            for (int i = 0; i < segs; i++)
            {
                float hpInSeg = Utils.Clamp(player.statLife - i * maxPerSeg, 0f, maxPerSeg);
                if (hpInSeg <= 0f)
                {
                    break;
                }

                float ratio = hpInSeg / maxPerSeg;
                float coreW = (segW - 2f * scale) * ratio;
                float x = innerX + i * segW;

                Rectangle destCore = new Rectangle((int)x, (int)fillY, (int)coreW, (int)fillH);
                Rectangle srcCore = new Rectangle(0, 0, srcCoreW, fillTex.Height);
                sb.Draw(fillTex, destCore, srcCore, Color.White);

                Rectangle destCap = new Rectangle((int)(x + coreW), (int)fillY, (int)(2f * scale), (int)fillH);
                Rectangle srcCap = new Rectangle(srcCoreW, 0, 2, fillTex.Height);
                sb.Draw(fillTex, destCap, srcCap, Color.White);
            }

            // OVERLAY
            if (overlay != null)
            {
                Vector2 overlayPos = pos;
                if (Style == "Leaf") overlayPos.Y -= 4f;
                //if (Style == "Golden") return;
                if (Style == "Golden")
                {
                    overlayPos.Y -= 2f;
                    overlayPos.X -= 1f;
                }
                sb.Draw(overlay, overlayPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        [Obsolete("Use the new method instead which uses HorizontalBars instead.")]
        private void DrawVanillaHealthbar(Player player)
        {
            // Settings
            var sys = ModContent.GetInstance<HealthbarSystem>();
            float scale = sys.Scale;
            float offset = sys.Offset;
            var sb = Main.spriteBatch;

            // Health ratio
            float ratio = Utils.Clamp((float)player.statLife / player.statLifeMax2, 0f, 1f);
            if (ratio <= 0f) return;

            int barPixels = (int)(36f * ratio);
            if (barPixels < 3) barPixels = 3;

            // Color
            float v = ratio - 0.1f;
            float r = v > 0.5f ? 255f * (1f - v) * 2f : 255f;
            float g = v > 0.5f ? 255f : 255f * v * 2f;
            float a = 255f;
            float mul = 0.95f;
            r = MathHelper.Clamp(r * mul, 0f, 255f);
            g = MathHelper.Clamp(g * mul, 0f, 255f);
            a = MathHelper.Clamp(a * mul, 0f, 255f);
            Color color = new((byte)r, (byte)g, 0, (byte)a);

            // Textures
            Texture2D back = TextureAssets.Hb2.Value;
            Texture2D fill = TextureAssets.Hb1.Value;

            // Position
            int w = 36;
            int h = 12;
            Vector2 pos = player.Top + new Vector2(-w * scale / 2f, offset - h * scale) - Main.screenPosition;
            pos = new Vector2((int)pos.X, (int)pos.Y);

            // Extra offset
            pos.Y -= 10*scale;

            // Draw bar vanilla
            if (barPixels < 34)
            {
                if (barPixels < 36)
                    Main.spriteBatch.Draw(back, pos + new Vector2(barPixels * scale, 0f), new Rectangle(2, 0, 2, back.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                if (barPixels < 34)
                    Main.spriteBatch.Draw(back, pos + new Vector2((barPixels + 2) * scale, 0f), new Rectangle(barPixels + 2, 0, 36 - barPixels - 2, back.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                if (barPixels > 2)
                    Main.spriteBatch.Draw(fill, pos, new Rectangle(0, 0, barPixels - 2, fill.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(fill, pos + new Vector2((barPixels - 2) * scale, 0f), new Rectangle(32, 0, 2, fill.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            else
            {
                if (barPixels < 36)
                    Main.spriteBatch.Draw(back, pos + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, back.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(fill, pos, new Rectangle(0, 0, barPixels, fill.Height), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            //// Draw ruler lines
            //Texture2D px = TextureAssets.MagicPixel.Value;
            //int tickCount = 4;
            //int barPixelWidth = (int)(36f * scale);
            //int tickSpacing = barPixelWidth / (tickCount + 1);
            //int tickHeight = (int)(fill.Height * scale);
            //Color tickColor = Color.DarkGreen * 0.5f;

            //for (int i = 1; i <= tickCount; i++)
            //{
            //    float tickX = pos.X + tickSpacing * i;
            //    Rectangle tickRect = new Rectangle((int)tickX, (int)pos.Y, 3, tickHeight);
            //    Main.spriteBatch.Draw(px, tickRect, tickColor);
            //}
        }

    }
}
