using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.State;

public abstract class ResizableUIState : UIState
{
    private int lastW;
    private int lastH;
    private float lastScale;

    protected virtual UIState CreatePreviousState()
    {
        return new MainMenuTPVPABrowserUIState();
    }

    public override void OnActivate()
    {
        base.OnActivate();
        Capture();
        Recalculate();
        OnResized();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
            GoBackToTPVPABrowserState();

        if (lastW == Main.screenWidth && lastH == Main.screenHeight && lastScale == Main.UIScale)
            return;

        Capture();
        Recalculate();
        OnResized();
    }

    protected void GoBackToTPVPABrowserState()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);

        UIState previous = CreatePreviousState();
        previous.Activate();

        MainMenuSystem menu = ModContent.GetInstance<MainMenuSystem>();
        menu.ui?.SetState(previous);
    }

    private void Capture()
    {
        lastW = Main.screenWidth;
        lastH = Main.screenHeight;
        lastScale = Main.UIScale;
    }

    protected virtual void OnResized()
    {
    }
}