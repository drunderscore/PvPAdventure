using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; 
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Features.AdventureTeleport;

[Autoload(Side = ModSide.Client)]
public class AdventureTeleportSystem : ModSystem
{
    private UserInterface ui;
    internal AdventureTeleportState adventureTeleportState;

    public override void Load()
    {
        if (!Main.dedServ)
        {
            ui = new UserInterface();
            adventureTeleportState = new AdventureTeleportState();
            adventureTeleportState.Activate();

            Main.OnPostFullscreenMapDraw += DrawOverFullscreenMap;
        }
    }

    public override void Unload()
    {
        if (!Main.dedServ)
            Main.OnPostFullscreenMapDraw -= DrawOverFullscreenMap;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null) return;

        bool visible = AdventureTeleportStateSettings.GetIsEnabled() && Main.mapFullscreen;

        if (visible)
        {
            if (ui.CurrentState != adventureTeleportState)
                ui.SetState(adventureTeleportState);

            ui.Update(gameTime);

            if (adventureTeleportState.IsMouseOverUI)
                Main.LocalPlayer.mouseInterface = true;
        }
        else if (ui.CurrentState != null)
        {
            ui.SetState(null);
        }
    }

    // Draw AFTER the map, with our own Begin/End for UI
    private void DrawOverFullscreenMap(Vector2 _mapPos, float _mapScale)
    {
        if (ui?.CurrentState == null) return;

        var sb = Main.spriteBatch;
        bool began = false;

        try
        {
            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                effect: null,
                transformMatrix: Main.UIScaleMatrix
            );
            began = true;

            // draw our UI state over the fullscreen map
            ui.Draw(sb, Main._drawInterfaceGameTime);
        }
        finally
        {
            if (began)
                sb.End();
        }
    }

}
