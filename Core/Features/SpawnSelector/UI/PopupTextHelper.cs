using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.UI;
public static class PopupTextHelper
{
    public static void NewText(string text, Player player, Color color=default)
    {
        // Check if the config allows popup text
        var config = ModContent.GetInstance<AdventureClientConfig>();
        if (!config.ShowPopupText)
            return;

        // Default color to Crimson if none is provided
        if (color == default)
            color = Color.Crimson;

        // Create and display the popup text
        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = color,
            Text = text,
            Velocity = new(0f, -4),
            DurationInFrames = 120
        }, player.Top + new Vector2(0,-4));
    }
}
