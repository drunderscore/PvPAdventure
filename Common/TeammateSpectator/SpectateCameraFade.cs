using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator;

/// <summary>
/// Visual fade-in effect when the camera is moved a large distance in spectate mode. This is to prevent motion sickness/lag from sudden camera jumps.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class SpectateCameraFade : ModSystem
{
    private const float FadeDistanceTiles = 70f; // the distance where fades will actually be executed
    private const int FadeTicks = 42; // the number of ticks the fade will last for, 42 is 0.7 seconds at 60 fps
    private static readonly float FadeDistancePixelsSq = FadeDistanceTiles * 16f * FadeDistanceTiles * 16f;

    private static int fadeTicksLeft;
    private static bool hasLastPosition;
    private static Vector2 lastPosition;
    public static void SetScreenPosition(Vector2 position, bool allowFade = false)
    {
        Vector2 comparePosition = hasLastPosition ? lastPosition : Main.screenPosition;

        if (allowFade && fadeTicksLeft <= 0 && Vector2.DistanceSquared(comparePosition, position) >= FadeDistancePixelsSq)
        {
            if (ModContent.GetInstance<ClientConfig>().ShowCameraFade)
            {
                fadeTicksLeft = FadeTicks;
            }
        }

        hasLastPosition = true;
        lastPosition = position;
        Main.screenPosition = position;
    }

    public static void Reset()
    {
        fadeTicksLeft = 0;
        hasLastPosition = false;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (fadeTicksLeft > 0)
            fadeTicksLeft--;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Spectate Camera Fade",
            DrawFade,
            InterfaceScaleType.UI));
    }

    private static bool DrawFade()
    {
        if (fadeTicksLeft <= 0)
            return true;

        float progress = fadeTicksLeft / (float)FadeTicks;
        float alpha = progress * progress;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * alpha);
        return true;
    }

}
