using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Draws teammates' heads on the fullscreen map when hovering them.
/// Also allows teleportation to teammates via <see cref="SpawnAndSpectateHooks"/> 
/// </summary>
internal class PlayerHeadsOnMap : ModSystem
{
    public override void Load()
    {
        Main.OnPostFullscreenMapDraw += DrawHighlightedPlayerOnMap;
    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= DrawHighlightedPlayerOnMap;
    }

    private void DrawHighlightedPlayerOnMap(Vector2 mapOffset, float mapScale)
    {
        if (!Main.mapFullscreen) return;

        if (SpawnAndSpectateSystem.HoveringWorldSpawn)
        {
            Vector2 iconBottomCenter = new(
                (Main.spawnTileX + 0.5f) * mapScale + mapOffset.X,
                Main.spawnTileY * mapScale + mapOffset.Y
            );

            Vector2 textPos = iconBottomCenter + new Vector2(0f, 8f * Main.UIScale);

            string label = Language.GetTextValue("UI.SpawnPoint");
            float textScale = 1f * Main.UIScale;

            // Anchor centered horizontally, top-aligned vertically.
            Main.spriteBatch.Begin();
            Utils.DrawBorderString(
                Main.spriteBatch,
                label,
                textPos,
                Color.White,
                textScale,
                anchorx: 0.5f,
                anchory: 0f
            );
            Main.spriteBatch.End();
        }



        if (SpawnAndSpectateSystem.HoveredPlayerIndex is not int idx)
            return;

        if (idx < 0 || idx >= Main.maxPlayers) return;

        Player player = Main.player[idx];
        if (player == null || !player.active || player.dead) return;

        float tileX = (player.position.X + player.width * 0.5f) / 16f;
        float tileY = (player.position.Y + player.gfxOffY + player.height * 0.5f) / 16f;

        float x = tileX * mapScale + mapOffset.X;
        float y = tileY * mapScale + mapOffset.Y;

        // Magic numbers from vanilla, adjusted a bit...
        x -= 7f;
        y -= 3f;
        //y -= 2f - mapScale / 5f * 2f;
        //x -= 10f * mapScale;
        //y -= 10f * mapScale;

        Vector2 headPos = new(x, y);

        float alpha = 1f;
        float scale = 1.6f*Main.UIScale;
        if (Main.UIScale > 1.5) 
            scale = 1.6f * Main.UIScale * 0.9f;
        Color border = Main.GetPlayerHeadBordersColor(player);

        Main.spriteBatch.Begin();

        // Draw player head
        Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, headPos, alpha, scale, border);

        // Draw teleport to player text (perfectly centered under the head)
        string name = player.name;

        // headPos is slightly left/top adjusted already.
        Vector2 textAnchor = headPos + new Vector2(7f * Main.UIScale, 30f * Main.UIScale);

        Utils.DrawBorderString(
            Main.spriteBatch,
            name,
            textAnchor,
            Color.White,
            scale: 1f * Main.UIScale,
            anchorx: 0.5f,
            anchory: 0f
        );

        Main.spriteBatch.End();
    }
}
