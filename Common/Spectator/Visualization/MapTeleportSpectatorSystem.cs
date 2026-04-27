using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;
public class TeleportMapSystem : ModSystem
{
    // variables

    public override void Load()
    {
        Main.OnPostFullscreenMapDraw += TP_Map;
    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= TP_Map;
    }

    /// <summary>
    /// Draws a little text in bottom left corner of the screen to inform the player about the teleportation feature when map is open.
    /// </summary>
    public override void PostDrawFullscreenMap(ref string mouseText)
    {
        //var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
        //if (!spectatorConfig.MapTeleportAllowed)
        //return;

        if (!IsPlayerInSpectateMode())
            return;

        string teleportText = "Right click to teleport";
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(teleportText);
        Vector2 position = new(10f, Main.screenHeight - textSize.Y - 10f);
        Utils.DrawBorderString(Main.spriteBatch, teleportText, position, Color.White);
    }

    // NOTE: not working in multiplayer
    // NOTE UPDATE: it DOES work in multiplayer
    private void TP_Map(Vector2 arg1, float arg2)
    {
        //var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
        //if (!spectatorConfig.MapTeleportAllowed)
        //return;

        if (!IsPlayerInSpectateMode())
            return;

        if (Main.mouseRight)
        {
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight) * Main.UIScale;
            Vector2 target = ((Main.MouseScreen - screenSize / 2) / 16 * (16 / Main.mapFullscreenScale) + Main.mapFullscreenPos) * 16;

            if (WorldGen.InWorld((int)target.X / 16, (int)target.Y / 16))
            {
                Main.LocalPlayer.Center = target;
                Main.LocalPlayer.fallStart = (int)Main.LocalPlayer.position.Y;
            }
            else
            {
                Log.Chat("Error: outside world bounds when trying to teleport");
            }
        }
    }

    private static bool IsPlayerInSpectateMode()
    {
        return Main.mapFullscreen &&
            Main.LocalPlayer?.active == true &&
            SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer);
    }
}