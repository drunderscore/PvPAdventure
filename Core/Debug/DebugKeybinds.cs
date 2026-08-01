#if DEBUG
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

[Autoload(Side = ModSide.Client)]
internal sealed class DebugKeybinds : ModSystem
{
    internal static readonly Color MessageColor = new(255, 190, 70);

    private bool numPad1Released = true;

    public override void OnWorldLoad() =>
        numPad1Released = true;

    public override void PostUpdateEverything()
    {
        if (Main.gameMenu ||
            !PressedWithShift(
                Keys.NumPad1,
                ref numPad1Released))
        {
            return;
        }

        DebugStatsSystem.ToggleFromKeybind();
    }

    private static bool PressedWithShift(
        Keys key,
        ref bool released)
    {
        if (Main.keyState.IsKeyUp(key))
        {
            released = true;
            return false;
        }

        if (!released || !Main.keyState.IsKeyDown(key))
            return false;

        released = false;

        bool shift =
            Main.keyState.IsKeyDown(Keys.LeftShift) ||
            Main.keyState.IsKeyDown(Keys.RightShift);

        bool control =
            Main.keyState.IsKeyDown(Keys.LeftControl) ||
            Main.keyState.IsKeyDown(Keys.RightControl);

        bool alt =
            Main.keyState.IsKeyDown(Keys.LeftAlt) ||
            Main.keyState.IsKeyDown(Keys.RightAlt);

        return shift && !control && !alt;
    }
}
#endif
