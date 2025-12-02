using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.TeamSelector;

[Autoload(Side = ModSide.Client)]
internal class TeamSelectorSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState teamSelectorState;

    // State
    private bool Active { get; set; }
    public bool IsActive() => Active;
    public bool ToggleActive() => Active = !Active;

    public override void OnWorldLoad()
    {
        // Initialize UI and state
        ui = new();
        teamSelectorState = new();

        // Initialize content in the state
        TeamSelectorPanel teamSelectorPanel = new();
        teamSelectorState.Append(teamSelectorPanel);
        ui.SetState(teamSelectorState);
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
                name: "PvPAdventure: TeamSelectorSystem",
                drawMethod: () => 
                { 
                    if (IsActive())
                    {
                        ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
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
