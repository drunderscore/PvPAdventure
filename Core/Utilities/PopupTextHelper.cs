using Microsoft.Xna.Framework;
using PvPAdventure.Common.SSC;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Utilities;

public static class PopupTextHelper
{
    //public static bool IsEnabled => ModContent.GetInstance<ClientConfig>().

    /// <summary> Shows popup text above the local player. </summary>
    public static void NewText(string localizationKey)
    {
        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = Color.Crimson,
            Text = Language.GetTextValue(localizationKey),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 60 * 2
        }, Main.LocalPlayer.Top + new Vector2(0f, -40f));
    }
}
