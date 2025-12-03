using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.HerosMod.TeamSelector;

[Autoload(Side = ModSide.Client)]
internal class TeamSelectorSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState teamSelectorState;

    // State
    public bool IsActive() => ui?.CurrentState == teamSelectorState;
    public void ToggleActive() => ui.SetState(IsActive() ? null : teamSelectorState);

    public override void OnWorldLoad()
    {
        ui = new();
        teamSelectorState = new();
        teamSelectorState.Append(new TeamSelectorElement());
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
