using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem_v2;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Draws teammates' heads on the fullscreen map when hovering them.
/// </summary>
internal class PlayerHeadsOnMap : ModSystem
{
    internal static bool MapHoverWorldSpawn;
    internal static int? MapHoverPlayerIndex;

    public override void Load()
    {
        Main.OnPostFullscreenMapDraw += DrawPlayerHeadOnMap;
    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= DrawPlayerHeadOnMap;
    }

    private void DrawPlayerHeadOnMap(Vector2 mapOffset, float mapScale)
    {
        if (!Main.mapFullscreen)
        {
            MapHoverWorldSpawn = false;
            MapHoverPlayerIndex = null;
            return;
        }

        bool drawWorld = MapHoverWorldSpawn;
        bool drawPlayer = MapHoverPlayerIndex is int idx &&
                          idx >= 0 && idx < Main.maxPlayers &&
                          Main.player[idx] != null &&
                          Main.player[idx].active &&
                          !Main.player[idx].dead;

        if (!drawWorld && !drawPlayer)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise);

        if (drawWorld)
        {
            Vector2 iconBottomCenter = new(
                (Main.spawnTileX + 0.5f) * mapScale + mapOffset.X,
                Main.spawnTileY * mapScale + mapOffset.Y
            );

            Vector2 textPos = iconBottomCenter + new Vector2(0f, 8f * Main.UIScale);
            string label = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn");

            Utils.DrawBorderString(
                Main.spriteBatch,
                label,
                textPos,
                Color.White,
                scale: 1f * Main.UIScale,
                anchorx: 0.5f,
                anchory: 0f
            );
        }

        if (drawPlayer)
        {
            Player player = Main.player[MapHoverPlayerIndex!.Value];

            float tileX = (player.position.X + player.width * 0.5f) / 16f;
            float tileY = (player.position.Y + player.gfxOffY + player.height * 0.5f) / 16f;

            Vector2 headPos = new(tileX * mapScale + mapOffset.X - 7f, tileY * mapScale + mapOffset.Y - 3f);

            float headScale = 1.6f * Main.UIScale;
            if (Main.UIScale > 1.5f)
                headScale *= 0.9f;

            Color border = Main.GetPlayerHeadBordersColor(player);
            Vector2 textAnchor = headPos + new Vector2(7f * Main.UIScale, 30f * Main.UIScale);

            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, headPos, alpha: 1f, headScale, border);

            Utils.DrawBorderString(
                Main.spriteBatch,
                player.name,
                textAnchor,
                Color.White,
                scale: 1f * Main.UIScale,
                anchorx: 0.5f,
                anchory: 0f
            );
        }

        Main.spriteBatch.End();
    }

}
