using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.TeamAssigner;

/// <summary>
/// The system responsible for managing the team selector UI.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class TeamAssignerSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState teamAssignerState;

    // State
    public bool IsActive() => ui?.CurrentState == teamAssignerState;
    public void ToggleActive() => ui.SetState(IsActive() ? null : teamAssignerState);

    public override void OnWorldLoad()
    {
        ui = new();
        teamAssignerState = new();
        teamAssignerState.Append(new TeamAssignerElement());
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
                name: "PvPAdventure: TeamAssignerSystem",
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
