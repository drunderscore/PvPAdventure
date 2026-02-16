using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Tools.ArenasTool;

[Autoload(Side = ModSide.Client)]
internal sealed class ArenasAdminSystem : ModSystem
{
    public UserInterface ui;
    public UIState uiState;

    public bool IsActive()
    {
        return ui?.CurrentState == uiState;
    }

    public void ToggleActive()
    {
        if (ui == null)
            return;

        ui.SetState(IsActive() ? null : uiState);
    }

    public void Hide()
    {
        ui?.SetState(null);
    }

    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        uiState = new UIState();
        uiState.Append(new ArenasAdminPanel());
        ui.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            name: "PvPAdventure: ArenasAdminSystem",
            drawMethod: () =>
            {
                if (IsActive())
                    ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);

                return true;
            },
            scaleType: InterfaceScaleType.UI
        ));
    }
}
