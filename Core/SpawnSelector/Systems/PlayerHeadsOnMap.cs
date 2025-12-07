using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnSelector.Systems;

/// <summary>
/// Draws the selected player heads on map when highlighting the UI element for the player
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

        int idx = SpawnSelectorSystem.HoveredPlayerIndex;
        if (idx < 0 || idx >= Main.maxPlayers) return;

        Player player = Main.player[idx];
        if (player == null || !player.active || player.dead) return;

        float tileX = (player.position.X + player.width * 0.5f) / 16f;
        float tileY = (player.position.Y + player.gfxOffY + player.height * 0.5f) / 16f;

        float x = tileX * mapScale + mapOffset.X;
        float y = tileY * mapScale + mapOffset.Y;

        x -= 7f;
        y -= 3f;
        //y -= 2f - mapScale / 5f * 2f;
        //x -= 10f * mapScale;
        //y -= 10f * mapScale;

        Vector2 headPos = new Vector2(x, y);

        float alpha = 1f;
        float scale = 1.6f*Main.UIScale;
        if (Main.UIScale > 1.5) 
            scale = 1.6f * Main.UIScale * 0.9f;
        Color border = Main.GetPlayerHeadBordersColor(player);

        Main.spriteBatch.Begin();

        // Draw player head
        Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, headPos, alpha, scale, border);

        // Draw teleport to player text
        string name = player.name;
        Vector2 size = FontAssets.MouseText.Value.MeasureString(name);
        Vector2 pos = headPos + new Vector2(-size.X * 0.5f+7*Main.UIScale, 30f*Main.UIScale);
        Utils.DrawBorderString(Main.spriteBatch, name, pos, Color.White, 1f*Main.UIScale);

        Main.spriteBatch.End();
    }
}
