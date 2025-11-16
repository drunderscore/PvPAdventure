using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.UI;
public static class PopupTextHelper
{
    public static void NewText(string text, Color color=default, bool showInMultiplayer=false)
    {
        if (color == default)
            color = Color.Crimson;

        var config = ModContent.GetInstance<AdventureClientConfig>();
        if (!config.ShowPopupText)
        {
            return;
        }

        if (showInMultiplayer)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = color,
                Text = text,
                Velocity = new(0f, -4f),
                DurationInFrames = 30
            }, Main.LocalPlayer.Top);
            return;
        }

        if (Main.LocalPlayer.whoAmI == Main.myPlayer)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = color,
                Text = text,
                Velocity = new(0f, -4f),
                DurationInFrames = 60
            }, Main.LocalPlayer.Top);
        }
    }
}
