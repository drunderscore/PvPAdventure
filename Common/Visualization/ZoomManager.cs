using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization;

/// <summary>
/// Caps the screen zoom based on the player's screen resolution, using 1080p as the baseline.
/// </summary>
public class ZoomManager : ModSystem
{
    private const float BaselineHeight = 1080f;

    public override void Load()
    {
        if (Main.dedServ) return;
        On_Main.DoUpdate += OnDoUpdate;
    }

    public override void Unload() { }

    private void OnDoUpdate(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);
        Main.GameZoomTarget = Math.Max(1.0f, Main.screenHeight / BaselineHeight * 1.0f);
    }
}