using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features;

public class TeleportMapSystem : ModSystem
{
    public static class RTPSpawnSelectorSettings
    {
        public static bool IsEnabled = false;
    }

    public override void Load()
    {
        Main.OnPostFullscreenMapDraw += TP_Map;
    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= TP_Map;
    }

    public override void PostDrawFullscreenMap(ref string mouseText)
    {
        if (!RTPSpawnSelectorSettings.IsEnabled)
            return;

        string teleportText = "PvP Adventure Teleport Options\n1. Click here to random teleport\n2. Click teammate to teleport";

        // Load panel
        Texture2D panelTexture = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;
        int cornerSize = 12;
        int barSize = 4;
        Color panelColor = new Color(63, 82, 151) * 0.7f;

        // Panel bounds
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(teleportText);
        Vector2 textPosition = new(Main.screenWidth / 2 - textSize.X / 2, 20f);
        Rectangle panelBounds = new((int)textPosition.X - 10, (int)textPosition.Y - 8, (int)textSize.X + 20, (int)textSize.Y + 12);

        // Check if mouse is hovering
        bool isHovering = panelBounds.Contains(Main.mouseX, Main.mouseY);
        Color textColor = isHovering ? Color.Yellow : Color.White;

        // Draw panel
        DrawPanel(Main.spriteBatch, panelBounds, panelTexture, cornerSize, barSize, panelColor);

        // Draw text with hover color
        Utils.DrawBorderString(Main.spriteBatch, teleportText, textPosition, textColor);

        // Optional: set mouseText to show tooltip
        if (isHovering)
        {
            mouseText = "Click to teleport";
        }
    }

    private static void RandomTeleport()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.LocalPlayer.TeleportationPotion();
        else
            NetMessage.SendData(MessageID.RequestTeleportationByServer);
    }

    private void TP_Map(Vector2 arg1, float arg2)
    {
        if (!RTPSpawnSelectorSettings.IsEnabled)
            return;

        string teleportText = "PvP Adventure Teleport Options\n1. Click here to random teleport\n2. Click teammate to teleport";
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(teleportText);
        Vector2 textPosition = new(Main.screenWidth / 2 - textSize.X / 2, 20f);
        Rectangle panelBounds = new((int)textPosition.X - 10, (int)textPosition.Y - 8, (int)textSize.X + 20, (int)textSize.Y + 12);

        // Check if mouse clicked inside panel bounds
        if (Main.mouseLeft && Main.mouseLeftRelease && panelBounds.Contains(Main.mouseX, Main.mouseY))
        {
            RandomTeleport(); // Trigger teleport

            // Close the map after teleporting
            Main.mapFullscreen = false;
            RTPSpawnSelectorSettings.IsEnabled = false;
        }
    }

    /// <summary>
    /// Helper to draw a blue panel
    /// </summary>
    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, Texture2D texture, int cornerSize, int barSize, Color color)
    {
        Point point = bounds.Location;
        Point point2 = new Point(bounds.Right - cornerSize, bounds.Bottom - cornerSize);
        int width = bounds.Width - cornerSize * 2;
        int height = bounds.Height - cornerSize * 2;

        // Corners
        spriteBatch.Draw(texture, new Rectangle(point.X, point.Y, cornerSize, cornerSize), new Rectangle(0, 0, cornerSize, cornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y, cornerSize, cornerSize), new Rectangle(cornerSize + barSize, 0, cornerSize, cornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(point.X, point2.Y, cornerSize, cornerSize), new Rectangle(0, cornerSize + barSize, cornerSize, cornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(point2.X, point2.Y, cornerSize, cornerSize), new Rectangle(cornerSize + barSize, cornerSize + barSize, cornerSize, cornerSize), color);

        // Edges
        spriteBatch.Draw(texture, new Rectangle(point.X + cornerSize, point.Y, width, cornerSize), new Rectangle(cornerSize, 0, barSize, cornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(point.X + cornerSize, point2.Y, width, cornerSize), new Rectangle(cornerSize, cornerSize + barSize, barSize, cornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(point.X, point.Y + cornerSize, cornerSize, height), new Rectangle(0, cornerSize, cornerSize, barSize), color);
        spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y + cornerSize, cornerSize, height), new Rectangle(cornerSize + barSize, cornerSize, cornerSize, barSize), color);

        // Center
        spriteBatch.Draw(texture, new Rectangle(point.X + cornerSize, point.Y + cornerSize, width, height), new Rectangle(cornerSize, cornerSize, barSize, barSize), color);
    }

    [Obsolete("Used for teleporting to the mouse cursor position, not useful in this case")]
    private void obsolete_TP_to_cursor()
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
            //Log.Info("Error: outside world bounds when trying to teleport");
        }
    }
}