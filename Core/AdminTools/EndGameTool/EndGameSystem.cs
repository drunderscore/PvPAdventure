using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.EndGameTool;

[Autoload(Side = ModSide.Client)]
internal class EndGameSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState endGameUIState;

    // State
    public bool IsActive() => ui?.CurrentState == endGameUIState;
    public void ToggleActive() => ui.SetState(IsActive() ? null : endGameUIState);

    public override void OnWorldLoad()
    {
        ui = new();
        endGameUIState = new();
        endGameUIState.Append(new EndGamePanel());

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
                name: "PvPAdventure: EndGameSystem",
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


