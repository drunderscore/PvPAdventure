using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.PointsSetter;

[Autoload(Side = ModSide.Client)]
internal class PointsSetterSystem : ModSystem
{
    public UserInterface ui;
    public UIState pointsSetterState;
    public PointsSetterPanel pointsSetterElement;

    public bool IsActive() => ui?.CurrentState == pointsSetterState;

    public void ToggleActive()
    {
        if (IsActive())
        {
            ui.SetState(null);
            return;
        }

        ui.SetState(pointsSetterState);
    }
    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        pointsSetterState = new UIState();
        pointsSetterElement = new PointsSetterPanel();
        pointsSetterState.Append(pointsSetterElement);
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
            name: "PvPAdventure: PointsSetterSystem",
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
