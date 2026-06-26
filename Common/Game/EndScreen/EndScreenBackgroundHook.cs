using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Replaces only the vanilla background while the end screen is visible.</summary>
public class EndScreenBackgroundHook : ModSystem
{
    private Hook drawBgHook;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        MethodInfo method = typeof(Main).GetMethod("DrawBG", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
            drawBgHook = new Hook(method, new Action<Action<Main>, Main>(DrawBGDetour));
    }

    public override void Unload()
    {
        drawBgHook?.Dispose();
        drawBgHook = null;
    }

    private static void DrawBGDetour(Action<Main> orig, Main self)
    {
        EndScreenSystem system = ModContent.GetInstance<EndScreenSystem>();
        if (system?.IsVisible != true)
        {
            orig(self);
            return;
        }

        float opacity = system.Opacity;
        Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        Main.spriteBatch.Draw(pixel, screen, new Color(2, 1, 9) * opacity); // only replace vanilla sky
    }
}
