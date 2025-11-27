using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using PvPAdventure.Core.SpawnSelector.Players;
using PvPAdventure.System;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using static PvPAdventure.System.RegionManager;
using static Terraria.ModLoader.BackupIO;

namespace PvPAdventure.Core.Spawnbox;

[Autoload(Side = ModSide.Client)]
public class SpawnBoxWorld : ModSystem
{
    // Initialize the interface layer.
    private readonly SpawnBoxInterfaceLayer _randomTeleportGameInterfaceLayer = new();
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
        if (!layers.Contains(_randomTeleportGameInterfaceLayer))
        {
            // add at the bottom interface layer
            // https://github.com/tModLoader/tModLoader/wiki/Vanilla-Interface-layers-values
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _randomTeleportGameInterfaceLayer);
        }
    }

    private class SpawnBoxInterfaceLayer() : GameInterfaceLayer("PvPAdventure: Spawn Box", InterfaceScaleType.UI)
    {
        protected override bool DrawSelf()
        {
            //var hitbox = Main.LocalPlayer.Hitbox;
            //var tileHitbox = new Rectangle(hitbox.X / 16, hitbox.Y / 16, hitbox.Width / 16, hitbox.Height / 16);
            DrawSpawnBox(Main.spriteBatch);

            return base.DrawSelf();
        }

        private static void DrawSpawnBox(SpriteBatch sb)
        {
            var rm = ModContent.GetInstance<RegionManager>();
            var bgTexture = ModContent.GetInstance<SpawnBoxWorld>()._playerBGTexture;
            if (rm.Regions.Count == 0 || bgTexture == null)
                return;

            Region region = rm.Regions[0];
            Rectangle area = region.Area;

            Vector2 worldTopLeft = new(area.X * 16, area.Y * 16);
            Vector2 worldSize = new(area.Width * 16, area.Height * 16);

            Vector2 screenTopLeft = worldTopLeft - Main.screenPosition;

            float uiScale = Main.UIScale;
            screenTopLeft /= uiScale;
            worldSize /= uiScale;

            Rectangle r = new(
                (int)screenTopLeft.X,
                (int)screenTopLeft.Y,
                (int)worldSize.X,
                (int)worldSize.Y
            );

            // Adjust to fit player inside the box (slightly) better
            int fullTile = (int) (16f / Main.UIScale);
            int halfTile = (int)(fullTile * 0.5f);
            r.X -= halfTile;
            r.Y -= fullTile;
            r.Height += halfTile;
            r.Width += halfTile;

            int thickness = (int)Math.Max(1f, 16f / uiScale); // 1 tile wide in world

            float opacity = 1.0f;

            // If we're playing and outside the ring, we can't move outside
            var gm = ModContent.GetInstance<GameManager>();
            if (gm.CurrentPhase == GameManager.Phase.Playing)
            {
                opacity = 0.7f;

                var am = Main.LocalPlayer.GetModPlayer<AdventureMirrorPlayer>();
                if (!am.IsPlayerInSpawnRegion())
                {
                    opacity = 1f;
                }
            }

            DrawOutline(sb, r, thickness, Color.Black * opacity);
        }

        private static void DrawOutline(SpriteBatch sb, Rectangle r, int t, Color col)
        {
            Texture2D pix = Terraria.GameContent.TextureAssets.MagicPixel.Value;

            // top
            sb.Draw(pix, new Rectangle(r.X, r.Y, r.Width, t), col);
            // bottom
            sb.Draw(pix, new Rectangle(r.X, r.Y + r.Height - t, r.Width, t), col);
            // left
            sb.Draw(pix, new Rectangle(r.X, r.Y, t, r.Height), col);
            // right
            sb.Draw(pix, new Rectangle(r.X + r.Width - t, r.Y, t, r.Height), col);
        }


        private void DrawSpawnBoxPretty(SpriteBatch sb)
        {
            var rm = ModContent.GetInstance<RegionManager>();
            var bgTexture = ModContent.GetInstance<SpawnBoxWorld>()._playerBGTexture;

            if (rm.Regions.Count == 0 || bgTexture == null)
            {
                return;
            }

            // Get the area
            Region region = rm.Regions[0];
            Rectangle area = region.Area;

            Vector2 topLeft = new(area.X * 16, area.Y * 16);
            Vector2 areaSize = new(area.Width * 16, area.Height * 16);

            // Convert to screen pixel coords
            Vector2 screenTopLeft = topLeft - Main.screenPosition;

            // Compensate for UI scaling (PostDrawInterface uses Main.UIScaleMatrix)
            float uiScale = Main.UIScale;
            screenTopLeft /= uiScale;
            areaSize /= uiScale;

            Rectangle screenRect = new Rectangle(
                (int)screenTopLeft.X,
                (int)screenTopLeft.Y,
                (int)areaSize.X,
                (int)areaSize.Y
            );

            // Draw the nine-slice border
            DrawNineSliceBorder(
                sb,
                screenRect.X,
                screenRect.Y,
                screenRect.Width,
                screenRect.Height,
                bgTexture.Value,
                Color.Black,
                inset: -16,
                c: 10
            );
        }

        private static void DrawNineSliceBorder(SpriteBatch sb, int x, int y, int w, int h, Texture2D tex, Color color, int inset, int c = 5)
        {
            c = 12;
            x += inset; y += inset; w -= inset * 2 - 16; h -= inset * 2;
            int ew = tex.Width - c * 2;
            int eh = tex.Height - c * 2;

            // corners + edges, NO center
            sb.Draw(tex, new Rectangle(x, y, c, c), new Rectangle(0, 0, c, c), color);
            sb.Draw(tex, new Rectangle(x + c, y, w - c * 2, c), new Rectangle(c, 0, ew, c), color);
            sb.Draw(tex, new Rectangle(x + w - c, y, c, c), new Rectangle(tex.Width - c, 0, c, c), color);

            sb.Draw(tex, new Rectangle(x, y + c, c, h - c * 2), new Rectangle(0, c, c, eh), color);
            // center row skipped here
            sb.Draw(tex, new Rectangle(x + w - c, y + c, c, h - c * 2), new Rectangle(tex.Width - c, c, c, eh), color);

            sb.Draw(tex, new Rectangle(x, y + h - c, c, c), new Rectangle(0, tex.Height - c, c, c), color);
            sb.Draw(tex, new Rectangle(x + c, y + h - c, w - c * 2, c), new Rectangle(c, tex.Height - c, ew, c), color);
            sb.Draw(tex, new Rectangle(x + w - c, y + h - c, c, c), new Rectangle(tex.Width - c, tex.Height - c, c, c), color);
        }
    }
}
