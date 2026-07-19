using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Tools.GameManagerTool;

[Autoload(Side = ModSide.Client)]
internal class GameManagerUISystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState timerUIState;

    // State
    public bool IsActive() => ui?.CurrentState != null;

    public void ShowExtendGameDialog() => ui.SetState(timerUIState);
    public void ShowStartDialog() => ui.SetState(timerUIState);
    public void Hide() => ui.SetState(null);

    public override void OnWorldLoad()
    {
        ui = new();
        timerUIState = new();
        timerUIState.Append(new GameManagerPanel());

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
                name: "PvPAdventure: GameManagerSystem",
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


