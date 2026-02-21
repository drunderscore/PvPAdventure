using Microsoft.Xna.Framework;
using PvPAdventure.Common.ReeseRecorder.MainMenuUI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class ReeseMainMenuSystem : ModSystem
{
    private UserInterface ui;
    private ReeseMainMenuState state;

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        ui = new UserInterface();
        state = new ReeseMainMenuState();
        ui.SetState(state);

        On_Main.DrawMenu += DrawMenu;
        On_Main.UpdateUIStates += UpdateUIStates;
    }

    private void DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        if (Main.gameMenu && Main.menuMode == 0 && ui?.CurrentState != null)
            ui.Draw(Main.spriteBatch, gameTime);

        orig(self, gameTime);
    }

    private void UpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        if (Main.gameMenu && Main.menuMode == 0)
        {
            if (ui.CurrentState == null)
                ui.SetState(state);

            ui.Update(gameTime);
        }
        else if (ui.CurrentState != null)
        {
            ui.SetState(null);
        }

        orig(gameTime);
    }

    public override void Unload()
    {
        On_Main.DrawMenu -= DrawMenu;
        On_Main.UpdateUIStates -= UpdateUIStates;

        ui = null;
        state = null;
    }
}