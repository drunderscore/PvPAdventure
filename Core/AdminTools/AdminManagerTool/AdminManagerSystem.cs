using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.AdminManagerTool;

/// <summary>
/// The system responsible for managing admins UI.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class AdminManagerSystem : ModSystem
{
    // Components
    public UserInterface ui;
    public UIState adminManagerState;

    // State
    public bool IsActive() => ui?.CurrentState == adminManagerState;
    public void ToggleActive() => ui.SetState(IsActive() ? null : adminManagerState);

    public override void OnWorldLoad()
    {
        ui = new();
        adminManagerState = new();
        adminManagerState.Append(new AdminManagerPanel());
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
                name: "PvPAdventure: AdminManagerSystem",
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
