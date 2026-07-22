using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Utilities;

public static class PopupTextHelper
{
    public static bool Enabled => ModContent.GetInstance<ClientConfig>().ShowPortalWarnings;

    /// <summary>Shows localized popup text above the local player.</summary>
    public static void NewLocalizedText(string localizationKey) =>
        Show(Main.LocalPlayer, Language.GetTextValue(localizationKey), Color.Crimson, -40f);

    /// <summary>Shows localized popup text above the specified local player.</summary>
    public static void NewLocalizedText(Player player, string localizationKey, Color color = default) =>
        Show(player, Language.GetTextValue(localizationKey), color, -4f);

    /// <summary>Shows literal popup text above the specified local player.</summary>
    public static void NewText(Player player, string text, Color color = default) =>
        Show(player, text, color, -4f);

    private static void Show(Player player, string text, Color color, float verticalOffset)
    {
        if (!Enabled || player?.whoAmI != Main.myPlayer || string.IsNullOrEmpty(text))
            return;

        if (color == default)
            color = player.team >= 0 && player.team < Main.teamColor.Length
                ? Main.teamColor[player.team]
                : Color.Crimson;

        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = color,
            Text = text,
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 60 * 2
        }, player.Top + new Vector2(0f, verticalOffset));
    }
}
