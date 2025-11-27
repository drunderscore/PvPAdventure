using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using PvPAdventure.Core.SpawnSelector.Players;
using PvPAdventure.System;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using static PvPAdventure.System.RegionManager;

namespace PvPAdventure.Core.Spawnbox;

[Autoload(Side = ModSide.Client)]
public class SpawnBoxWorld : ModSystem
{
    // Initialize the interface layer.
    private readonly SpawnBoxInterfaceLayer _spawnBoxInterfaceLayer = new();
    public Asset<Texture2D> _playerBGTexture;

    // Load the texture.
    public override void Load()
    {
        if (!Main.dedServ)
            _playerBGTexture = Ass.CustomPlayerBackground;
    }

    // Add the layer.
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_spawnBoxInterfaceLayer))
        {
            // add at the bottom interface layer
            // https://github.com/tModLoader/tModLoader/wiki/Vanilla-Interface-layers-values
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _spawnBoxInterfaceLayer);
        }
    }

    private class SpawnBoxInterfaceLayer() : GameInterfaceLayer("PvPAdventure: Spawn Box", InterfaceScaleType.UI)
    {
        protected override bool DrawSelf()
        {
            // To see how to draw coordinates properly, see the wiki on Coordinates.
            // https://github.com/tModLoader/tModLoader/wiki/Coordinates

            // Draw the spawn box.
            DrawSpawnBox(Main.spriteBatch);
            return base.DrawSelf();
        }

        private void DrawSpawnBox(SpriteBatch sb)
        {
            var rm = ModContent.GetInstance<RegionManager>();
            if (rm.Regions.Count == 0)
                return;

            Region region = rm.Regions[0];

            // 🔹 this is the “solid ring” bounds in TILE space
            Rectangle ring = region.CollisionRingBounds;

            int leftTile = ring.X;
            int rightTile = ring.X + ring.Width;   // exclusive
            int topTile = ring.Y;
            int bottomTile = ring.Y + ring.Height;  // exclusive

            // Tile -> world
            float leftWorld = leftTile * 16f;
            float rightWorld = rightTile * 16f;
            float topWorld = topTile * 16f;
            float bottomWorld = bottomTile * 16f;

            // World -> screen
            float uiScale = Main.UIScale;

            float leftScreen = (leftWorld - Main.screenPosition.X) / uiScale;
            float rightScreen = (rightWorld - Main.screenPosition.X) / uiScale;
            float topScreen = (topWorld - Main.screenPosition.Y) / uiScale;
            float bottomScreen = (bottomWorld - Main.screenPosition.Y) / uiScale;


            var gm = ModContent.GetInstance<GameManager>();
            var am = Main.LocalPlayer.GetModPlayer<AdventureMirrorPlayer>();

            float solidOpacity = 1.0f;
            float lightOpacity = 0.5f;

            float opacity = gm.CurrentPhase == GameManager.Phase.Playing
                ? (am.IsPlayerInSpawnRegion() ? lightOpacity : solidOpacity)
                : solidOpacity;

            int l = (int)leftScreen;
            int r = (int)rightScreen;
            int t = (int)topScreen;
            int b = (int)bottomScreen;

            int thickness = (int)Math.Max(1f, 16f / uiScale);
            //Texture2D pix = TextureAssets.MagicPixel.Value;
            //Color col = Color.Gray * 0.7f;

            // vanilla borders (top,left,bottom,right)
            //sb.Draw(pix, new Rectangle(l, t, r - l, thickness), col);
            //sb.Draw(pix, new Rectangle(l, b - thickness, r - l, thickness), col);
            //sb.Draw(pix, new Rectangle(l, t, thickness, b - t), col);
            //sb.Draw(pix, new Rectangle(r - thickness, t, thickness, b - t), col);

            Rectangle screenRect = new(l,t,r - l, b - t);
            DrawNineSliceBorder(sb, screenRect, Color.White * opacity);
        }

        private void DrawNineSliceBorder(SpriteBatch sb, Rectangle rect, Color color)
        {
            Texture2D tex = ModContent.GetInstance<SpawnBoxWorld>()._playerBGTexture.Value;


            tex = Ass.Spawnbox.Value;

            int c = 16; // corner size
            int x = rect.X;
            int y = rect.Y;
            int w = rect.Width;
            int h = rect.Height;

            int ew = tex.Width - c * 2;
            int eh = tex.Height - c * 2;

            // top-left
            sb.Draw(tex, new Rectangle(x, y, c, c), new Rectangle(0, 0, c, c), color);

            // top
            sb.Draw(tex, new Rectangle(x + c, y, w - c * 2, c), new Rectangle(c, 0, ew, c), color);

            // top-right
            sb.Draw(tex, new Rectangle(x + w - c, y, c, c), new Rectangle(tex.Width - c, 0, c, c), color);

            // left
            sb.Draw(tex, new Rectangle(x, y + c, c, h - c * 2), new Rectangle(0, c, c, eh), color);

            // right
            sb.Draw(tex, new Rectangle(x + w - c, y + c, c, h - c * 2), new Rectangle(tex.Width - c, c, c, eh), color);

            // bottom-left
            sb.Draw(tex, new Rectangle(x, y + h - c, c, c), new Rectangle(0, tex.Height - c, c, c), color);

            // bottom
            sb.Draw(tex, new Rectangle(x + c, y + h - c, w - c * 2, c),new Rectangle(c, tex.Height - c, ew, c), color);

            // bottom-right
            sb.Draw(tex, new Rectangle(x + w - c, y + h - c, c, c),new Rectangle(tex.Width - c, tex.Height - c, c, c), color);
        }
    }
}
