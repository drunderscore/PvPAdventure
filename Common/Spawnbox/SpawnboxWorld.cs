using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spawnbox;

[Autoload(Side = ModSide.Client)]
public sealed class SpawnBoxWorld : ModSystem
{
    private const float TileSize = 16f;
    private static readonly Color BlockedColor = new(255, 80, 80);
    private static readonly Color PassableColor = new(70, 226, 158);

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
        if (index != -1)
            layers.Insert(index + 1, new SpawnBoxInterfaceLayer());
    }

    private sealed class SpawnBoxInterfaceLayer() : GameInterfaceLayer("PvPAdventure: SpawnBox", InterfaceScaleType.Game)
    {
        protected override bool DrawSelf()
        {
            DrawSpawnBox(Main.spriteBatch);
            return true;
        }

        private static void DrawSpawnBox(SpriteBatch spriteBatch)
        {
            if (Main.dedServ || Main.gameMenu)
                return;

            SpawnBoxSystem box = ModContent.GetInstance<SpawnBoxSystem>();
            Rectangle inner = GetScreenRect(box.TileArea);
            int thickness = box.Thickness * (int)TileSize;

            if (inner.Width <= 0 || inner.Height <= 0 || thickness <= 0)
                return;

            if (EffectLoader.TryGetSpawnBoxBorderEffect(out Effect effect))
                DrawShaderBorder(spriteBatch, inner, thickness, effect);
            else
                DrawPixelBorder(spriteBatch, inner, thickness);
        }

        private static Rectangle GetScreenRect(Rectangle area)
        {
            Vector2 screenTopLeft = new(area.Left * TileSize, area.Top * TileSize);
            Vector2 screenBottomRight = new(area.Right * TileSize, area.Bottom * TileSize);

            screenTopLeft -= Main.screenPosition;
            screenBottomRight -= Main.screenPosition;

            Rectangle rect = new(
                (int)Math.Floor(screenTopLeft.X),
                (int)Math.Floor(screenTopLeft.Y),
                (int)Math.Round(screenBottomRight.X - screenTopLeft.X),
                (int)Math.Round(screenBottomRight.Y - screenTopLeft.Y));

            return rect;
        }

        private static bool CanLocalPlayerCross(SpawnBoxSystem box) =>
            box.CanExit && box.TouchesWorldHitbox(Main.LocalPlayer.Hitbox);

        private static Color GetDrawColor(SpawnBoxSystem box) => CanLocalPlayerCross(box) ? PassableColor : BlockedColor;

        private static float GetDrawOpacity(SpawnBoxSystem box) => CanLocalPlayerCross(box) ? 0.5f : 0.88f;

        private static void DrawShaderBorder(SpriteBatch sb, Rectangle inner, int thickness, Effect effect)
        {
            SpawnBoxSystem box = ModContent.GetInstance<SpawnBoxSystem>();
            Rectangle outer = inner;
            outer.Inflate(thickness, thickness);

            if (outer.Width <= 0 || outer.Height <= 0)
                return;

            effect.Parameters["globalTime"]?.SetValue((float)Main.timeForVisualEffects);
            effect.Parameters["borderColor"]?.SetValue(GetDrawColor(box).ToVector3());
            effect.Parameters["opacity"]?.SetValue(GetDrawOpacity(box));
            effect.Parameters["borderSize"]?.SetValue(new Vector2(thickness / (float)outer.Width, thickness / (float)outer.Height));
            effect.Parameters["outerEdgeFade"]?.SetValue(0.42f);
            effect.Parameters["innerEdgeFade"]?.SetValue(0.30f);
            effect.Parameters["pulseStrength"]?.SetValue(0.12f);
            effect.Parameters["shimmerStrength"]?.SetValue(0.08f);
            effect.Parameters["shimmerScale"]?.SetValue(34f);
            effect.Parameters["shimmerSpeed"]?.SetValue(0.07f);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            sb.Draw(TextureAssets.MagicPixel.Value, outer, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        }

        private static void DrawPixelBorder(SpriteBatch sb, Rectangle inner, int thickness)
        {
            SpawnBoxSystem box = ModContent.GetInstance<SpawnBoxSystem>();
            Color color = GetDrawColor(box) * GetDrawOpacity(box);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            sb.Draw(pixel, new Rectangle(inner.X - thickness, inner.Y - thickness, inner.Width + thickness * 2, thickness), color);
            sb.Draw(pixel, new Rectangle(inner.X - thickness, inner.Bottom, inner.Width + thickness * 2, thickness), color);
            sb.Draw(pixel, new Rectangle(inner.X - thickness, inner.Y, thickness, inner.Height), color);
            sb.Draw(pixel, new Rectangle(inner.Right, inner.Y, thickness, inner.Height), color);
        }
    }
}
