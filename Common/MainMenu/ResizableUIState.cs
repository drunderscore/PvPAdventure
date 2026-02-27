using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu;

public abstract class ResizableUIState : UIState
{
    private int _lastW;
    private int _lastH;
    private float _lastScale;

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

        if (_lastW == Main.screenWidth && _lastH == Main.screenHeight && _lastScale == Main.UIScale)
            return;

        Capture();
        Recalculate();
        OnResized();
    }

    private void Capture()
    {
        _lastW = Main.screenWidth;
        _lastH = Main.screenHeight;
        _lastScale = Main.UIScale;
    }

    protected virtual void OnResized() { }
}