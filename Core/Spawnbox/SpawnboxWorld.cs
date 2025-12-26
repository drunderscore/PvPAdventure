using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.Core.SpawnAndSpectate;
using PvPAdventure.System;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spawnbox;

/// <summary>
/// Draws the spawn box rectangle in the world.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SpawnBoxWorld : ModSystem
{
    public Asset<Texture2D> _playerBGTexture;

    public override void Load()
    {
        if (!Main.dedServ)
            _playerBGTexture = Ass.CustomPlayerBackground;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        // Insert just after a vanilla UI layer so it's always on top of the world.
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
        if (idx != -1)
        {
            layers.Insert(idx + 1, new SpawnBoxInterfaceLayer());
        }
    }

    private sealed class SpawnBoxInterfaceLayer : GameInterfaceLayer
    {
        public SpawnBoxInterfaceLayer()
            : base("PvPAdventure: SpawnBox", InterfaceScaleType.Game)
        {
        }

        protected override bool DrawSelf()
        {
            DrawSpawnBox(Main.spriteBatch);
            return true;
        }

        private void DrawSpawnBox(SpriteBatch spriteBatch)
        {
            if (Main.dedServ)
                return;

            // Set the spawnbox size.
            int leftTile = Main.spawnTileX - 25;
            int topTile = Main.spawnTileY - 25;
            int rightTile = leftTile + 50;
            int bottomTile = topTile + 50;

            // Get world coordinates.
            Vector2 worldTopLeft = new(leftTile * 16f, topTile * 16f);
            Vector2 worldBottomRight = new(rightTile * 16f, bottomTile * 16f);

            // Convert world coordinates to screen cordinates.
            Vector2 screenTopLeft = worldTopLeft - Main.screenPosition;
            Vector2 screenBottomRight = worldBottomRight - Main.screenPosition;

            int x = (int)Math.Floor(screenTopLeft.X);
            int y = (int)Math.Floor(screenTopLeft.Y);
            int w = (int)Math.Round(screenBottomRight.X - screenTopLeft.X);
            int h = (int)Math.Round(screenBottomRight.Y - screenTopLeft.Y);
            if (w <= 0 || h <= 0)
                return;

            Rectangle rect = new(x, y, w, h);
            rect.Inflate(16, 16);

            DrawNineSliceBorder(spriteBatch, rect);
        }

        private void DrawNineSliceBorder(SpriteBatch sb, Rectangle rect)
        {
            // Set color
            var gm = ModContent.GetInstance<GameManager>();
            var am = Main.LocalPlayer.GetModPlayer<SpawnAndSpectatePlayer>();
            bool canPass = gm.CurrentPhase == GameManager.Phase.Playing && am.IsPlayerInSpawnRegion();
            Color color = Color.Black * (canPass ? 0.5f : 1f);

            Texture2D tex = Ass.Spawnbox.Value;
            const int srcCorner = 16;
            int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
            int srcEdgeWidth = tex.Width - srcCorner * 2;
            int srcEdgeHeight = tex.Height - srcCorner * 2;
            int dstCorner = srcCorner;

            sb.Draw(tex, new Rectangle(x, y, dstCorner, dstCorner), new Rectangle(0, 0, srcCorner, srcCorner), color);
            sb.Draw(tex, new Rectangle(x + dstCorner, y, w - dstCorner * 2, dstCorner), new Rectangle(srcCorner, 0, srcEdgeWidth, srcCorner), color);
            sb.Draw(tex, new Rectangle(x + w - dstCorner, y, dstCorner, dstCorner), new Rectangle(tex.Width - srcCorner, 0, srcCorner, srcCorner), color);

            sb.Draw(tex, new Rectangle(x, y + dstCorner, dstCorner, h - dstCorner * 2), new Rectangle(0, srcCorner, srcCorner, srcEdgeHeight), color);
            sb.Draw(tex, new Rectangle(x + w - dstCorner, y + dstCorner, dstCorner, h - dstCorner * 2), new Rectangle(tex.Width - srcCorner, srcCorner, srcCorner, srcEdgeHeight), color);

            sb.Draw(tex, new Rectangle(x, y + h - dstCorner, dstCorner, dstCorner), new Rectangle(0, tex.Height - srcCorner, srcCorner, srcCorner), color);
            sb.Draw(tex, new Rectangle(x + dstCorner, y + h - dstCorner, w - dstCorner * 2, dstCorner), new Rectangle(srcCorner, tex.Height - srcCorner, srcEdgeWidth, srcCorner), color);
            sb.Draw(tex, new Rectangle(x + w - dstCorner, y + h - dstCorner, dstCorner, dstCorner), new Rectangle(tex.Width - srcCorner, tex.Height - srcCorner, srcCorner, srcCorner), color);
        }
    }
}
