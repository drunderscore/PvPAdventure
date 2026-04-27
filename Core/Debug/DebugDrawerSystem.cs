using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal sealed class DebugDrawerSystem : ModSystem
{
    private UserInterface debugInterface;
    private UIState debugState;
    private DebugContentPanel debugPanel;

    public override void OnWorldLoad()
    {
        debugInterface = new UserInterface();
        debugState = new UIState();

        debugPanel = new DebugContentPanel();
        debugState.Append(debugPanel);
        debugState.Activate();

        debugInterface.SetState(debugState);
    }

    public override void OnWorldUnload()
    {
        debugPanel = null;
        debugState = null;
        debugInterface = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.keyState.IsKeyDown(Keys.NumPad5) && !Main.oldKeyState.IsKeyDown(Keys.NumPad5))
            debugPanel?.ToggleActiveAndRebuild();

        debugInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Debug UI",
            () =>
            {
                debugInterface?.Draw(Main.spriteBatch, new GameTime());

                DebugDrawer.DrawButtons();
                DebugDrawer.DrawDebugInfo();
                DebugDrawer.Flush(Main.spriteBatch);

                return true;
            },
            InterfaceScaleType.UI));
    }
}
#endif