using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Travel.UI;

[Autoload(Side =ModSide.Client)]
internal class TravelUISystem : ModSystem
{
    private UserInterface travelUI;
    public TravelUIState travelUIState;

    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.dedServ || travelUI == null || travelUIState == null)
            return;

        Player local = Main.LocalPlayer;
        bool show = TravelTeleportSystem.ShouldShowTravelUI(local);

        if (!show)
        {
            if (travelUI.CurrentState != null)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                travelUI.SetState(null);
                travelUIState.ForceRebuildNextUpdate();
                TravelTeleportSystem.ClearSelection();
            }

            return;
        }

        if (travelUI.CurrentState != travelUIState)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            travelUIState.ForceRebuildNextUpdate();
            travelUI.SetState(travelUIState);
        }

        travelUIState.RebuildIfNeeded();
        travelUI.Update(gameTime);
    }

    #region Load hooks
    public override void OnWorldLoad()
    {
        travelUI = new();
        travelUIState = new();

        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }
    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }
    #endregion

    #region Drawing

    private void DrawOnFullscreenMap(Vector2 mapPos, float mapScale)
    {
        if (!Main.mapFullscreen || travelUI?.CurrentState == null)
            return;

        SpriteBatch sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        travelUI.Draw(sb, Main._drawInterfaceGameTime);
        sb.End();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        // TESTME: Draw the UI below the config?
        if (IsAnyConfigUIOpen())
            idx = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (idx == -1)
            return;

        layers.Insert(idx, new LegacyGameInterfaceLayer(
            "PvPAdventure: TravelUISystem",
            delegate
            {
                if (Main.mapFullscreen || travelUI?.CurrentState == null)
                    return true;

                SpriteBatch sb = Main.spriteBatch;
                travelUI.Draw(sb, Main._drawInterfaceGameTime);
                return true;
            },
            InterfaceScaleType.UI
        ));
    }
    #endregion

    #region Helpers
    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return Main.ingameOptionsWindow || s is UIModConfig or UIModConfigList;
    }
    public static bool IsMouseHovering
    {
        get
        {
            var system = ModContent.GetInstance<TravelUISystem>();
            return system?.travelUI?.CurrentState != null &&
                   system.travelUIState?.backgroundPanel?.IsMouseHovering == true;
        }
    }
    #endregion
}
