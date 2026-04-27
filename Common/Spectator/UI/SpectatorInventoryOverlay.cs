using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI;

internal static class SpectatorInventoryOverlay
{
    private static Player player;

    public static bool IsOpen(Player target)
    {
        return ReferenceEquals(player, target);
    }

    public static void Toggle(Player target)
    {
        player = IsOpen(target) ? null : target;
    }

    public static void Clear()
    {
        player = null;
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        if (player?.active != true)
        {
            player = null;
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);
        InventoryDrawer.DrawInventory(spriteBatch, new Vector2(20f, 20f), player, viewport);
        InventoryDrawer.DrawEquipment(spriteBatch, player, viewport);
    }
}