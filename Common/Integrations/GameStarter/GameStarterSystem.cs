using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.GameStarter;

[Autoload(Side = ModSide.Client)]
internal class GameStarterSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState endGameUIState;
    public UIState startGameUIState;

    // State
    public bool IsActive() => ui?.CurrentState != null;

    public void ShowEndDialog() => ui.SetState(endGameUIState);
    public void ShowStartDialog() => ui.SetState(startGameUIState);
    public void Hide() => ui.SetState(null);

    public override void OnWorldLoad()
    {
        ui = new();
        endGameUIState = new();
        endGameUIState.Append(new EndGameElement());

        startGameUIState = new();
        startGameUIState.Append(new GameStarterElement());

        ui.SetState(null);
    }
    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);
        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index != -1)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                name: "PvPAdventure: StartGameSystem",
                drawMethod: () =>
                {
                    if (IsActive())
                    {
                        ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);

                        // Debug
                        //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Red * 0.5f);

                        return true;
                    }
                    return true;
                },
                scaleType: InterfaceScaleType.UI
            ));
        }
    }
}


