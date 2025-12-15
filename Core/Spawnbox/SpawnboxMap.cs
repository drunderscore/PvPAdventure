using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnSelector;
using PvPAdventure.System;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.ModLoader;
using static PvPAdventure.System.RegionManager;

namespace PvPAdventure.Core.Spawnbox;

public class SpawnboxMap : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        var rm = ModContent.GetInstance<RegionManager>();
        if (rm.Regions.Count == 0)
            return;

        DrawSpawnBoxOnFullscreenMap(ref context);
    }

    private static void DrawSpawnBoxOnFullscreenMap(ref MapOverlayDrawContext context)
    {

        if (Main.mapFullscreenScale < 0.5) return;

        // spawn region in tiles
        //Rectangle area = new(Main.spawnTileX - 25, Main.spawnTileY - 25, 50, 50);

        // Get the spawn region from RegionManager
        var rm = ModContent.GetInstance<RegionManager>();
        var bgTexture = ModContent.GetInstance<SpawnBoxWorld>()._playerBGTexture;
        if (rm.Regions.Count == 0 || bgTexture == null)
            return;

        Region region = rm.Regions[0];
        Rectangle area = region.Area;

        // Convert from tiles to map screen
        Vector2 topLeft = (new Vector2(area.X, area.Y) - context.MapPosition) * context.MapScale + context.MapOffset;
        Vector2 size = new(area.Width * context.MapScale, area.Height * context.MapScale);

        int x = (int)topLeft.X;
        int y = (int)topLeft.Y;
        int w = (int)size.X;
        int h = (int)size.Y;

        // Create rectangle for spawn area
        Rectangle spawnRect = new(x, y, w, h);

        // For minimap only: clip to minimap bounds
        if (!Main.mapFullscreen && Main.mapStyle == 1)
        {
            Rectangle minimapRect = new(Main.miniMapX, Main.miniMapY, Main.miniMapWidth, Main.miniMapHeight);

            spawnRect = Rectangle.Intersect(spawnRect, minimapRect);

            // If completely outside, don't draw at all
            if (spawnRect.Width <= 0 || spawnRect.Height <= 0)
                return;

            x = spawnRect.X;
            y = spawnRect.Y;
            w = spawnRect.Width;
            h = spawnRect.Height;
        }

        var gm = ModContent.GetInstance<GameManager>();
        var am = Main.LocalPlayer.GetModPlayer<SpawnSelectorPlayer>();
        bool canPass = gm.CurrentPhase == GameManager.Phase.Playing
          && am.IsPlayerInSpawnRegion();

        // now draw using the (possibly clipped) rect
        Texture2D pix = TextureAssets.MagicPixel.Value;
        Color col = Color.Black;
        if (canPass) col = Color.Black * 0.5f;
        int thickness = 2; 

        if (Main.mapFullscreen)
            thickness = (int)(1 * Main.mapFullscreenScale); // match 1 pixel at scale 1

        // top, bottom, left, right
        Main.spriteBatch.Draw(pix,new Rectangle(x + thickness, y, w - thickness * 2, thickness),col);
        Main.spriteBatch.Draw(pix,new Rectangle(x + thickness, y + h - thickness, w - thickness * 2, thickness),col);
        Main.spriteBatch.Draw(pix,new Rectangle(x, y, thickness, h),col);
        Main.spriteBatch.Draw(pix,new Rectangle(x + w - thickness, y, thickness, h),col);

        var font = FontAssets.DeathText.Value;

        float scale = context.MapScale * context.DrawScale*0.3f;

        Vector2 textSize = font.MeasureString("SPAWN") * scale;

        Vector2 textPos = new Vector2(
            x + w / 2f - textSize.X / 2f,
            y + h / 2f - textSize.Y / 2f
        );
        textPos.Y -= 50*scale;

        // Helper to draw text
        void drawSpawnText(Vector2 offset, Color c)
        {
            Main.spriteBatch.DrawString(
                font,
                "SPAWN",
                textPos + offset,
                c,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }

        //drawSpawnText(new Vector2(2, 2) * scale, Color.Black * 0.75f);
        //drawSpawnText(Vector2.Zero, Color.White);
    }
}
