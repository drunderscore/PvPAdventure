using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spawnbox.SpawnBoxTool;

[Autoload(Side = ModSide.Client)]
public class SpawnBoxToolSystem : ModSystem
{
    private UserInterface _ui;
    private UIState _state;

    public bool IsActive() => _ui?.CurrentState == _state;

    public void ToggleActive() => _ui?.SetState(IsActive() ? null : _state);

    public override void OnWorldLoad()
    {
        _ui = new UserInterface();
        _state = new UIState();
        _state.Append(new SpawnBoxPanel());
    }

    public override void UpdateUI(GameTime gameTime) => _ui?.Update(gameTime);

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpawnBoxTool",
            () =>
            {
                if (IsActive())
                    _ui?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);

                return true;
            },
            InterfaceScaleType.UI));
    }
}
